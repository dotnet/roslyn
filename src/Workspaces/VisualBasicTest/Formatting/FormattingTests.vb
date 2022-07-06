' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Formatting
    Public Class FormattingTest
        Inherits VisualBasicFormattingTestBase

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Format1() As Task
            Dim code = <Code>                   
    Namespace                   A                               
            End                 Namespace                           </Code>

            Dim expected = <Code>
Namespace A
End Namespace</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function NamespaceBlock() As Task
            Dim code = <Code>                           Namespace                   A                               
    Class            C
                End             Class
            End                 Namespace                           </Code>

            Dim expected = <Code>Namespace A
    Class C
    End Class
End Namespace</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TypeBlock() As Task
            Dim code = <Code>    Class            C         
                Sub             Method          (           )          
        End             Sub
                End             Class       
                           </Code>

            Dim expected = <Code>Class C
    Sub Method()
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function MethodBlock() As Task
            Dim code = <Code>    Class            C         
                Sub             Method          (           )          
Dim             a           As          Integer             =           1               
        End             Sub
                End             Class       
                           </Code>

            Dim expected = <Code>Class C
    Sub Method()
        Dim a As Integer = 1
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function StructBlock() As Task
            Dim code = <Code>    Structure            C         
                Sub             Method          (           )          
Dim             a           As          Integer             =           1               
        End             Sub

      Dim field As Integer
                End             Structure       
                           </Code>

            Dim expected = <Code>Structure C
    Sub Method()
        Dim a As Integer = 1
    End Sub

    Dim field As Integer
End Structure
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function EnumBlock() As Task
            Dim code = <Code>    Enum            C         
A           
                         B  
                                X
     Z
                End             Enum       </Code>

            Dim expected = <Code>Enum C
    A
    B
    X
    Z
End Enum</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ModuleBlock() As Task
            Dim code = <Code>        Module module1     
Sub   goo()        
 End      Sub  
                End                 Module        </Code>

            Dim expected = <Code>Module module1
    Sub goo()
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function InterfaceBlock() As Task
            Dim code = <Code>      Interface        IGoo  
    Sub   goo()  
  End Interface    
</Code>

            Dim expected = <Code>Interface IGoo
    Sub goo()
End Interface
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function PropertyBlock() As Task
            Dim code = <Code>               Class           C               
                        Property            P           (           )          As           Integer
    Get
                Return      1           
                        End                 Get
                Set         (       ByVal           value       As      Integer     )           
End          Set            
        End                 Property                    
                                            End             Class                   </Code>

            Dim expected = <Code>Class C
    Property P() As Integer
        Get
            Return 1
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function EventBlock() As Task
            Dim code = <Code>           Class           C               
                Public                  Custom          Event           MouseDown           As          EventHandler            
AddHandler          (               ByVal Value                 As EventHandler         )
    Dim             i                As              Integer            =           1               
                End             AddHandler              
        RemoveHandler           (               ByVal           Value           As      EventHandler            )           
                        Dim             i           As          Integer             =           1               
                                                                                                        End RemoveHandler
            RaiseEvent      (           ByVal       sender      As          Object,             ByVal           e       As          Object      )
                                Dim i As Integer = 1
                End RaiseEvent
End Event           
                End             Class               </Code>

            Dim expected = <Code>Class C
    Public Custom Event MouseDown As EventHandler
        AddHandler(ByVal Value As EventHandler)
            Dim i As Integer = 1
        End AddHandler
        RemoveHandler(ByVal Value As EventHandler)
            Dim i As Integer = 1
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As Object)
            Dim i As Integer = 1
        End RaiseEvent
    End Event
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WhileBlockNode() As Task
            Dim code = <Code>Class            C         
                Sub             Method          (           )          
While True
                                    Dim i As Integer = 1
                        End While
        End             Sub
                End             Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        While True
            Dim i As Integer = 1
        End While
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function UsingBlockNode() As Task
            Dim code = <Code>Class C
    Sub Method()
                    Using TraceSource
                                                Dim i = 1
                    End Using
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Using TraceSource
            Dim i = 1
        End Using
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function SyncLockBlockNode() As Task
            Dim code = <Code>Class C
    Sub Method()
                    SyncLock            New             Object              
Dim                 i           =               10              
                End             SyncLock            
        End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        SyncLock New Object
            Dim i = 10
        End SyncLock
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WithBlockNode() As Task
            Dim code = <Code>Class C
    Sub Method()
                    With            New             Object              
    Dim             i           As          Integer             =       1           
                        End             With            
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        With New Object
            Dim i As Integer = 1
        End With
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function IfBlockNode() As Task
            Dim code = <Code>Class C
    Sub Method()
If              True            Then           
                                                Dim i As Integer = 1
        ElseIf      True            Then            
                                        Dim i       As      Integer = 1         
                    Else
                                                    Dim i As Integer = 1            
End             If          
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        If True Then
            Dim i As Integer = 1
        ElseIf True Then
            Dim i As Integer = 1
        Else
            Dim i As Integer = 1
        End If
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TryCatchBlockNode() As Task
            Dim code = <Code>Class C
    Sub Method()
                Try           
                                                Dim i As Integer = 1
        Catch                  e       As      Exception           When     TypeOf      e       Is  ArgumentNullException       
        Try                                
        Dim i       As      Integer = 1         
            Catch    ex  As   ArgumentNullException    
 End   Try 
                    Catch e As Exception When TypeOf e Is ArgumentNullException
                                                    Dim i As Integer = 1            
   Finally         
  goo()
                        End         Try     
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Try
            Dim i As Integer = 1
        Catch e As Exception When TypeOf e Is ArgumentNullException
            Try
                Dim i As Integer = 1
            Catch ex As ArgumentNullException
            End Try
        Catch e As Exception When TypeOf e Is ArgumentNullException
            Dim i As Integer = 1
        Finally
            goo()
        End Try
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function SelectBlockNode() As Task
            Dim code = <Code>Class C
    Sub Method()
                                Dim i = 1

Select          Case            i               
                                            Case            1           ,           2           ,       3           
                                                    Dim             i2      =           1           
                        Case        1           To          3           
                    Dim i2 = 1
                                                    Case                Else            
        Dim i2 = 1
                                                    End             Select              
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Dim i = 1

        Select Case i
            Case 1, 2, 3
                Dim i2 = 1
            Case 1 To 3
                Dim i2 = 1
            Case Else
                Dim i2 = 1
        End Select
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function DoLoopBlockNode() As Task
            Dim code = <Code>Class C
    Sub Method()
                    Do
Dim i = 1
                    Loop
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Do
            Dim i = 1
        Loop
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function DoUntilBlockNode() As Task
            Dim code = <Code>Class C
    Sub  Method()    
 Do  Until    False  
   goo()    
  Loop  
    End    Sub   
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Do Until False
            goo()
        Loop
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ForBlockNode() As Task
            Dim code = <Code>Class C
    Sub Method()
                        For             i       =       1       To          10          Step            1               
Dim         a       =       1               
    Next
        End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        For i = 1 To 10 Step 1
            Dim a = 1
        Next
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function AnchorStatement() As Task
            Dim code = <Code>Imports System
                        Imports System.
                                    Collections.
                                        Generic</Code>

            Dim expected = <Code>Imports System
Imports System.
            Collections.
                Generic</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function AnchorQueryStatement1() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim a =                              From q In
                                                {1, 3, 5}
                                             Where q > 10
                                             Select q
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Dim a = From q In
                                                {1, 3, 5}
                Where q > 10
                Select q
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function AnchorQueryStatement2() As Task
            Dim code = <Code>Class C
    Sub Method()
      Dim a =   From q In
                   {1, 3, 5}
                Where q > 10
                Select q
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Dim a = From q In
                     {1, 3, 5}
                Where q > 10
                Select q
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function AnchorQueryStatement3() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim a =
                                             From q In
                                                {1, 3, 5}
                                             Where q > 10
                                             Select q
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Dim a =
                                             From q In
                                                {1, 3, 5}
                                             Where q > 10
                                             Select q
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function AlignQueryStatement() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim a =                              From q In                                                 {1, 3, 5}
                        Where q > 10
                                                                Select q
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Dim a = From q In {1, 3, 5}
                Where q > 10
                Select q
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Operators1() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim a = -           1
        Dim a2 = 1-1
        Dim a3 = +          1
        Dim a4 = 1+1
        Dim a5 = 2+(3*-2)
        Goo(2,(3))
    End Sub
End Class
</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Dim a = -1
        Dim a2 = 1 - 1
        Dim a3 = +1
        Dim a4 = 1 + 1
        Dim a5 = 2 + (3 * -2)
        Goo(2, (3))
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Operators2() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim myStr As String
myStr       =           "Hello"             &amp;               " World"            
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Dim myStr As String
        myStr = "Hello" &amp; " World"
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Operators3() As Task
            Dim code = <Code>Class C
    Sub Method()
        Dim a1 =    1    &lt;=  2
        Dim a2 = 2  &gt;=   3
        Dim a3 = 4  &lt;&gt;    5
        Dim a4 = 5 ^ 4
        Dim a5 = 0
        a5 +=1
        a5 -=1
        a5 *=1
        a5 /=1
        a5 \=1
        a5 ^=1
        a5&lt;&lt;= 1
        a5&gt;&gt;= 1
        a5 &amp;=    1
        a5 = a5&lt;&lt; 1
        a5 = a5&gt;&gt; 1
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Dim a1 = 1 &lt;= 2
        Dim a2 = 2 &gt;= 3
        Dim a3 = 4 &lt;&gt; 5
        Dim a4 = 5 ^ 4
        Dim a5 = 0
        a5 += 1
        a5 -= 1
        a5 *= 1
        a5 /= 1
        a5 \= 1
        a5 ^= 1
        a5 &lt;&lt;= 1
        a5 &gt;&gt;= 1
        a5 &amp;= 1
        a5 = a5 &lt;&lt; 1
        a5 = a5 &gt;&gt; 1
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Punctuation() As Task
            Dim code = <Code>   &lt;   Fact    (   )   , Trait (   Traits  .   Feature ,     Traits    .   Features    .   Formatting  )   &gt;    
Class A
End Class</Code>

            Dim expected = <Code>&lt;Fact(), Trait(Traits.Feature, Traits.Features.Formatting)&gt;
Class A
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Punctuation2() As Task
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Method(i := 1)
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Method(i:=1)
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Punctuation3() As Task
            Dim code = <Code><![CDATA[Class C
    <Attribute(goo := "value")>
    Sub Method()
    End Sub
End Class]]></Code>

            Dim expected = <Code><![CDATA[Class C
    <Attribute(goo:="value")>
    Sub Method()
    End Sub
End Class]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Lambda1() As Task
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    Dim         q       =           Function        (       t           )           1           
                    Dim             q2=Sub  (   t   )Console    .   WriteLine   (   t   )   
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q = Function(t) 1
        Dim q2 = Sub(t) Console.WriteLine(t)
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Lambda2() As Task
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q =             Function        (       t           )           
Return 1            
    End             Function            

        Dim         q2          =           Sub         (           t           )       
                            Dim             a       =           t           
                End         Sub             
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q = Function(t)
                    Return 1
                End Function

        Dim q2 = Sub(t)
                     Dim a = t
                 End Sub
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Lambda3() As Task
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
                                        Dim q =
                                            Function        (   t           )       
                                                        Return      1           
                    End             Function                

                                        Dim q2 =
                                            Sub     (       t           )                   
                    Dim             a           =           t           
        End          Sub
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q =
            Function(t)
                Return 1
            End Function

        Dim q2 =
            Sub(t)
                Dim a = t
            End Sub
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Lambda4() As Task
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
                                        Dim q =
                                            Function        (   t           )       
                                                        Return      1           
                    End             Function                

                                        Dim q2 =
                                            Sub     (       t           )                   
                    Dim             a           =           t           
  Dim  bbb     =    Function(r)    
          Return r
  End Function
        End          Sub
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q =
            Function(t)
                Return 1
            End Function

        Dim q2 =
            Sub(t)
                Dim a = t
                Dim bbb = Function(r)
                              Return r
                          End Function
            End Sub
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.Formatting)>
        <InlineData("_")>
        <InlineData("_ ' Comment")>
        Public Async Function LineContinuation1(continuation As String) As Task
            Dim code = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    Dim a = 1 + {continuation}
                            2 + {continuation}
                            3
    End Sub
End Class"

            Dim expected = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim a = 1 + {continuation}
                2 + {continuation}
                3
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.Formatting)>
        <InlineData("_")>
        <InlineData("_ ' Comment")>
        Public Async Function LineContinuation2(continuation As String) As Task
            Dim code = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    Dim aa = 1 + {continuation}
                             2 + {continuation}
                             3
    End Sub
End Class"

            Dim expected = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim aa = 1 + {continuation}
                 2 + {continuation}
                 3
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.Formatting)>
        <InlineData("_")>
        <InlineData("_ ' Comment")>
        Public Async Function LineContinuation3(continuation As String) As Task
            Dim code = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    Dim aa = 1 + {continuation}
    2 + {continuation}
    3
    End Sub
End Class"

            Dim expected = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim aa = 1 + {continuation}
2 + {continuation}
3
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.Formatting)>
        <InlineData("_")>
        <InlineData("_ ' Comment")>
        Public Async Function LineContinuation4(continuation As String) As Task
            Dim code = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    Dim aa = 1 + {continuation}
            {continuation}
                                          {continuation}
                                     {continuation}
    {continuation}
            {continuation}
    2 + {continuation}
    3
    End Sub
End Class"

            Dim expected = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim aa = 1 + {continuation}
                     {continuation}
                     {continuation}
                     {continuation}
                     {continuation}
                     {continuation}
2 + {continuation}
3
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.Formatting)>
        <InlineData("_")>
        <InlineData("_ ' Comment")>
        Public Async Function LineContinuation5(continuation As String) As Task
            Dim code = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim aa = 1 + {continuation}
            {continuation}
                                          {continuation}
                                     {continuation}
    {continuation}
            {continuation}
    2 + {continuation}
    3
    End Sub
End Class"

            Dim expected = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim aa = 1 + {continuation}
                     {continuation}
                     {continuation}
                     {continuation}
                     {continuation}
                     {continuation}
    2 + {continuation}
    3
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function AnonType() As Task
            Dim code = <Code>Class Goo
    Sub GooMethod()
                Dim SomeAnonType = New With {
                    .goo = "goo",
                 .answer = 42
                                }
    End Sub
End Class</Code>

            Dim expected = <Code>Class Goo
    Sub GooMethod()
        Dim SomeAnonType = New With {
            .goo = "goo",
         .answer = 42
                        }
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function CollectionInitializer() As Task
            Dim code = <Code>Class Goo
    Sub GooMethod()
        Dim somelist = New List(Of Integer) From {
            1,
                2,
        3
                        }
    End Sub
End Class</Code>

            Dim expected = <Code>Class Goo
    Sub GooMethod()
        Dim somelist = New List(Of Integer) From {
            1,
                2,
        3
                        }
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Label1() As Task
            Dim code = <Code>Class C
    Sub Method()
GoTo l
        l: Stop
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        GoTo l
l:      Stop
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Label2() As Task
            Dim code = <Code>Class C
    Sub Method()
GoTo goofoofoofoofoo
        goofoofoofoofoo:        Stop
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        GoTo goofoofoofoofoo
goofoofoofoofoo: Stop
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Label3() As Task
            Dim code = <Code>Class C
    Sub Goo()
        goo()
        x : goo()
        y :
        goo()
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Goo()
        goo()
x:      goo()
y:
        goo()
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Trivia1() As Task
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    ' Test
                                ' Test2
' Test 3
Dim a = 1
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        ' Test
        ' Test2
        ' Test 3
        Dim a = 1
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Trivia2() As Task
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    ''' Test
                                ''' Test2
''' Test 3
Dim a = 1
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        ''' Test
        ''' Test2
        ''' Test 3
        Dim a = 1
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.Formatting)>
        <InlineData("_")>
        <InlineData("_ ' Comment")>
        Public Async Function Trivia3(continuation As String) As Task
            Dim code = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
Dim a =             {continuation}
                {continuation}
                        {continuation}
        1
    End Sub
End Class"

            Dim expected = $"Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim a = {continuation}
                {continuation}
                {continuation}
                1
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <WorkItem(538354, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538354")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix3939() As Task
            Dim code = <Code>               
                        Imports System.
                                    Collections.    
                                        Generic             </Code>

            Dim expected = <Code>
Imports System.
            Collections.
                Generic</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538579")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4235() As Task
            Dim code = <Code>               
                #If False Then
                #End If
                    </Code>

            Dim expected = <Code>
#If False Then
#End If
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals1() As Task
            Dim code = "Dim xml =       <           XML         >               </         XML      >           "
            Dim expected = "        Dim xml = <XML></XML>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals2() As Task
            Dim code = "Dim xml =       <           XML         >    <%=            a            %>           </         XML      >           "
            Dim expected = "        Dim xml = <XML><%= a %></XML>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals3() As Task
            Dim code = "Dim xml =       <           local       :           XML         >               </         local            :           XML      >           "
            Dim expected = "        Dim xml = <local:XML></local:XML>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals4() As Task
            Dim code = "Dim xml =       <           local       :<%= hello %>         >               </               >           "
            Dim expected = "        Dim xml = <local:<%= hello %>></>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals5() As Task
            Dim code = "Dim xml =       <           <%= hello %>         >               </               >           "
            Dim expected = "        Dim xml = <<%= hello %>></>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals6() As Task
            Dim code = "Dim xml =       <           <%= hello %>         /> "
            Dim expected = "        Dim xml = <<%= hello %>/>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals7() As Task
            Dim code = "Dim xml =       <           xml            attr           =           ""1""           /> "
            Dim expected = "        Dim xml = <xml attr=""1""/>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals8() As Task
            Dim code = "Dim xml =       <           xml            attr           =           '1'           /> "
            Dim expected = "        Dim xml = <xml attr='1'/>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals9() As Task
            Dim code = "Dim xml =       <           xml            attr           =           '1'       attr2 = ""2""           attr3       =               <%=             hello               %>      /> "
            Dim expected = "        Dim xml = <xml attr='1' attr2=""2"" attr3=<%= hello %>/>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals10() As Task
            Dim code = "Dim xml =       <           xml            local:attr           =           '1'       attr2 = ""2""           attr3       =               <%=             hello               %>      /> "
            Dim expected = "        Dim xml = <xml local:attr='1' attr2=""2"" attr3=<%= hello %>/>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals11() As Task
            Dim code = "Dim xml =       <           xml            local:attr           =           '1'       <%= attr2 %>    = ""2""           local:<%= attr3 %>      =               <%=             hello               %>      /> "
            Dim expected = "        Dim xml = <xml local:attr='1' <%= attr2 %>=""2"" local:<%= attr3 %>=<%= hello %>/>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals12() As Task
            Dim code = "Dim xml =       <           xml>    test        </xml  > "
            Dim expected = "        Dim xml = <xml>    test        </xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals13() As Task
            Dim code = "Dim xml =       <           xml>    test                <%=         test            %>   </xml  > "
            Dim expected = "        Dim xml = <xml>    test                <%= test %></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals14() As Task
            Dim code = "Dim xml =       <           xml>    test                <%=         test            %>   <%=         test2            %>   </xml  > "
            Dim expected = "        Dim xml = <xml>    test                <%= test %><%= test2 %></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals15() As Task
            Dim code = "Dim xml =       <           xml>   <%=         test1            %>    test                <%=         test            %>   <%=         test2            %>   </xml  > "
            Dim expected = "        Dim xml = <xml><%= test1 %>    test                <%= test %><%= test2 %></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals16() As Task
            Dim code = "Dim xml =       <           xml>    <!--        test            -->         </xml  > "
            Dim expected = "        Dim xml = <xml><!--        test            --></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals17() As Task
            Dim code = "Dim         xml         =           <xml>     <test/>                   <!-- test -->    test            <!-- test -->              </xml>          "
            Dim expected = "        Dim xml = <xml><test/><!-- test -->    test            <!-- test --></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals18() As Task
            Dim code = "Dim         xml         =           <!-- test -->"
            Dim expected = "        Dim xml = <!-- test -->"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals19() As Task
            Dim code = "Dim         xml         =           <?xml-stylesheet            type = ""text/xsl""  href  =   ""show_book.xsl""?>"
            Dim expected = "        Dim xml = <?xml-stylesheet type = ""text/xsl""  href  =   ""show_book.xsl""?>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals20() As Task
            Dim code = "Dim         xml         =           <xml>     <test/>                   <!-- test -->    test            <!-- test -->      <?xml-stylesheet            type = 'text/xsl'  href  =   show_book.xsl?>        </xml>          "
            Dim expected = "        Dim xml = <xml><test/><!-- test -->    test            <!-- test --><?xml-stylesheet type = 'text/xsl'  href  =   show_book.xsl?></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals21() As Task
            Dim code = "Dim         xml         =           <xml>     <test/>                   <!-- test -->    test            <!-- test --> test 2 <?xml-stylesheet            type = 'text/xsl'  href  =   show_book.xsl?>        </xml>          "
            Dim expected = "        Dim xml = <xml><test/><!-- test -->    test            <!-- test --> test 2 <?xml-stylesheet type = 'text/xsl'  href  =   show_book.xsl?></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals22() As Task
            Dim code = "Dim         xml         =           <![CDATA[       Can contain literal <XML> tags      ]]>            "
            Dim expected = "        Dim xml = <![CDATA[       Can contain literal <XML> tags      ]]>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals23() As Task
            Dim code = "Dim         xml         =           <xml>     <test/>                   <!-- test -->    test    <![CDATA[       Can contain literal <XML> tags      ]]>        <!-- test --> test 2 <?xml-stylesheet            type = 'text/xsl'  href  =   show_book.xsl?>        </xml>          "
            Dim expected = "        Dim xml = <xml><test/><!-- test -->    test    <![CDATA[       Can contain literal <XML> tags      ]]><!-- test --> test 2 <?xml-stylesheet type = 'text/xsl'  href  =   show_book.xsl?></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals24() As Task
            Dim code = "Dim         xml         =           <xml>     <test>   <%=    <xml>     <test id=""42""><%=42   %>   <%=      ""hello""%>   </test>  </xml>   %>             </test>  </xml>"
            Dim expected = "        Dim xml = <xml><test><%= <xml><test id=""42""><%= 42 %><%= ""hello"" %></test></xml> %></test></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals25() As Task
            Dim code = "Dim         xml         =           <xml attr=""1"">    </xml>          "
            Dim expected = "        Dim xml = <xml attr=""1""></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals26() As Task
            Dim code = "
Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q = <xml>
    <hello>
    </hello>
</xml>
    End Sub
End Class"
            Dim expected = "
Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q = <xml>
                    <hello>
                    </hello>
                </xml>
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals27() As Task
            Dim code = "
Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q = <xml>
                    <!-- Test -->
                    <hello>
        Test
        <![CDATA[          ????             ]]>
                    </hello>
                    <!-- Test -->               </xml>
    End Sub
End Class"
            Dim expected = "
Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q = <xml>
                    <!-- Test -->
                    <hello>
        Test
        <![CDATA[          ????             ]]>
                    </hello>
                    <!-- Test --></xml>
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlLiterals28() As Task
            Dim code = "
Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q = <xml>           <!-- Test -->               <hello>
        Test
        <![CDATA[          ????             ]]>
    </hello>
    <!-- Test --></xml>
    End Sub
End Class"
            Dim expected = "
Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim q = <xml><!-- Test --><hello>
        Test
        <![CDATA[          ????             ]]>
                    </hello>
                    <!-- Test --></xml>
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function AttributeOnClass1() As Task
            Dim code = <Code><![CDATA[Namespace SomeNamespace
<SomeAttribute()>
    Class Goo
    End Class
End Namespace]]></Code>

            Dim expected = <Code><![CDATA[Namespace SomeNamespace
    <SomeAttribute()>
    Class Goo
    End Class
End Namespace]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(1087167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087167")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function MultipleAttributesOnClass() As Task
            Dim code = <Code><![CDATA[Namespace SomeNamespace
<SomeAttribute()>
            <SomeAttribute2()>
        Class Goo
    End Class
End Namespace]]></Code>

            Dim expected = <Code><![CDATA[Namespace SomeNamespace
    <SomeAttribute()>
    <SomeAttribute2()>
    Class Goo
    End Class
End Namespace]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(1087167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087167")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function MultipleAttributesOnParameter_1() As Task
            Dim code = <Code><![CDATA[Class Program
    Sub P(
                <Goo>
                        <Goo>
                    som As Integer)
    End Sub
End Class


Public Class Goo
    Inherits Attribute
End Class]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(1087167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087167")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function MultipleAttributesOnParameter_2() As Task
            Dim code = <Code><![CDATA[Class Program
    Sub P(
                        <Goo>
                    som As Integer)
    End Sub
End Class


Public Class Goo
    Inherits Attribute
End Class]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(1087167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087167")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function MultipleAttributesOnParameter_3() As Task
            Dim code = <Code><![CDATA[Class Program
    Sub P(     <Goo>
                        <Goo>
                    som As Integer)
    End Sub
End Class


Public Class Goo
    Inherits Attribute
End Class]]></Code>

            Dim expected = <Code><![CDATA[Class Program
    Sub P(<Goo>
                        <Goo>
                    som As Integer)
    End Sub
End Class


Public Class Goo
    Inherits Attribute
End Class]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function InheritsImplementsOnClass() As Task
            Dim code = <Code><![CDATA[Class SomeClass
Inherits BaseClass
        Implements IGoo

End Class]]></Code>

            Dim expected = <Code><![CDATA[Class SomeClass
    Inherits BaseClass
    Implements IGoo

End Class]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function InheritsImplementsWithGenericsOnClass() As Task
            Dim code = <Code><![CDATA[Class SomeClass(Of T)
Inherits BaseClass      (Of         T)
        Implements  IGoo     (      Of      String,
                              T)

End Class]]></Code>

            Dim expected = <Code><![CDATA[Class SomeClass(Of T)
    Inherits BaseClass(Of T)
    Implements IGoo(Of String,
                          T)

End Class]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function InheritsOnInterface() As Task
            Dim code = <Code>Interface I
Inherits J
End Interface

Interface J
End Interface</Code>

            Dim expected = <Code>Interface I
    Inherits J
End Interface

Interface J
End Interface</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WhitespaceMethodParens() As Task
            Dim code = <Code>Class SomeClass
    Sub Goo  (  x  As  Integer  )  
        Goo  (  42  )  
    End Sub
End Class</Code>

            Dim expected = <Code>Class SomeClass
    Sub Goo(x As Integer)
        Goo(42)
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WhitespaceKeywordParens() As Task
            Dim code = <Code>Class SomeClass
    Sub Goo
        If(x And(y Or(z)) Then Stop
    End Sub
End Class</Code>

            Dim expected = <Code>Class SomeClass
    Sub Goo
        If (x And (y Or (z)) Then Stop
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WhitespaceLiteralAndTypeCharacters() As Task
            Dim code = <Code><![CDATA[Class SomeClass
    Sub Method()
        Dim someHex = &HFF
        Dim someOct = &O33

        Dim someShort = 42S+42S
        Dim someInteger% = 42%+42%
        Dim someOtherInteger = 42I+42I
        Dim someLong& = 42&+42&
        Dim someOtherLong = 42L+42L
        Dim someDecimal@ = 42.42@+42.42@
        Dim someOtherDecimal = 42.42D  +  42.42D
        Dim someSingle! = 42.42!+42.42!
        Dim someOtherSingle = 42.42F+42.42F
        Dim someDouble# = 42.4242#+42.4242#
        Dim someOtherDouble = 42.42R+42.42R
        Dim unsignedShort = 42US+42US
        Dim unsignedLong = 42UL+42UL
        Dim someDate = #3/3/2011 12:42:00 AM#+#2/2/2011#
        Dim someChar = "x"c
        Dim someString$ = "42"+"42"
        Dim r = Goo&()
        Dim s = GooString$()
    End Sub
End Class]]></Code>

            Dim expected = <Code><![CDATA[Class SomeClass
    Sub Method()
        Dim someHex = &HFF
        Dim someOct = &O33

        Dim someShort = 42S + 42S
        Dim someInteger% = 42% + 42%
        Dim someOtherInteger = 42I + 42I
        Dim someLong& = 42& + 42&
        Dim someOtherLong = 42L + 42L
        Dim someDecimal@ = 42.42@ + 42.42@
        Dim someOtherDecimal = 42.42D + 42.42D
        Dim someSingle! = 42.42! + 42.42!
        Dim someOtherSingle = 42.42F + 42.42F
        Dim someDouble# = 42.4242# + 42.4242#
        Dim someOtherDouble = 42.42R + 42.42R
        Dim unsignedShort = 42US + 42US
        Dim unsignedLong = 42UL + 42UL
        Dim someDate = #3/3/2011 12:42:00 AM# + #2/2/2011#
        Dim someChar = "x"c
        Dim someString$ = "42" + "42"
        Dim r = Goo&()
        Dim s = GooString$()
    End Sub
End Class]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WhitespaceNullable() As Task
            Dim code = <Code>Class Goo
    Property someprop As Integer        ?

    Function Method(arg1        ? As Integer) As Integer            ?
        Dim someVariable        ? As Integer
    End Function
End Class</Code>

            Dim expected = <Code>Class Goo
    Property someprop As Integer?

    Function Method(arg1? As Integer) As Integer?
        Dim someVariable? As Integer
    End Function
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WhitespaceArrayBraces() As Task
            Dim code = <Code>Class Goo
    Sub Method()
        Dim arr()   ={   1,      2,          3       }       
    End Sub
End Class</Code>

            Dim expected = <Code>Class Goo
    Sub Method()
        Dim arr() = {1, 2, 3}
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WhitespaceStatementSeparator() As Task
            Dim code = <Code>Class Goo
    Sub Method()
        Dim x=2:Dim y=3:Dim z=4  
    End Sub
End Class</Code>

            Dim expected = <Code>Class Goo
    Sub Method()
        Dim x = 2 : Dim y = 3 : Dim z = 4
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WhitespaceRemovedBeforeComment() As Task
            Dim code = <Code>Class Goo
    Sub Method()
        Dim a = 4                           ' This is a comment that doesn't move
                                            ' This is a comment that will have some preceding whitespace removed
        Dim y = 4
    End Sub
End Class</Code>

            Dim expected = <Code>Class Goo
    Sub Method()
        Dim a = 4                           ' This is a comment that doesn't move
        ' This is a comment that will have some preceding whitespace removed
        Dim y = 4
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ReFormatWithTabsEnabled1() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        Goo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Goo()" + vbCrLf +
                vbTab + vbTab + "Goo()" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, True},
                {FormattingOptions2.TabSize, 4},
                {FormattingOptions2.IndentationSize, 4}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ReFormatWithTabsEnabled2() As Task
            ' tabs after the first token on a line should be converted to spaces

            Dim code =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Goo()" + vbCrLf +
                vbTab + vbTab + "Goo()" + vbTab + vbTab + "'comment" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Goo()" + vbCrLf +
                vbTab + vbTab + "Goo()       'comment" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, True},
                {FormattingOptions2.TabSize, 4},
                {FormattingOptions2.IndentationSize, 4}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ReFormatWithTabsEnabled3() As Task
            ' This is a regression test for the assert: it may still not pass after the assert is fixed.

            Dim code =
                "Class SomeClass" + vbCrLf +
                "        Sub Goo()           ' Comment" + vbCrLf +
                "  Goo()    " + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Goo()           ' Comment" + vbCrLf +
                vbTab + vbTab + "Goo()" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, True},
                {FormattingOptions2.TabSize, 4},
                {FormattingOptions2.IndentationSize, 4}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ReFormatWithTabsEnabled4() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        Dim abc = Sub()" + vbCrLf +
                "                      Console.WriteLine(42)" + vbCrLf +
                "                  End Sub" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Goo()" + vbCrLf +
                vbTab + vbTab + "Dim abc = Sub()" + vbCrLf +
                vbTab + vbTab + vbTab + vbTab + vbTab + "  Console.WriteLine(42)" + vbCrLf +
                vbTab + vbTab + vbTab + vbTab + "  End Sub" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, True},
                {FormattingOptions2.TabSize, 4},
                {FormattingOptions2.IndentationSize, 4}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ReFormatWithTabsEnabled5() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        Dim abc = 2 + " + vbCrLf +
                "                    3 + " + vbCrLf +
                "                   4 " + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Goo()" + vbCrLf +
                vbTab + vbTab + "Dim abc = 2 +" + vbCrLf +
                vbTab + vbTab + vbTab + vbTab + vbTab + "3 +" + vbCrLf +
                vbTab + vbTab + vbTab + vbTab + "   4" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, True},
                {FormattingOptions2.TabSize, 4},
                {FormattingOptions2.IndentationSize, 4}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(10742, "https://github.com/dotnet/roslyn/issues/10742")>
        Public Async Function ReFormatWithTabsEnabled6() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo() ' Comment" + vbCrLf +
                "        Goo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Goo() ' Comment" + vbCrLf +
                vbTab + vbTab + "Goo()" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, True},
                {FormattingOptions2.TabSize, 4},
                {FormattingOptions2.IndentationSize, 4}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ReFormatWithTabsDisabled() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Goo()" + vbCrLf +
                vbTab + vbTab + "Goo()" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        Goo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, False},
                {FormattingOptions2.TabSize, 4},
                {FormattingOptions2.IndentationSize, 4}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ReFormatWithDifferentIndent1() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        Goo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "  Sub Goo()" + vbCrLf +
                "    Goo()" + vbCrLf +
                "  End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, False},
                {FormattingOptions2.TabSize, 4},
                {FormattingOptions2.IndentationSize, 2}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ReFormatWithDifferentIndent2() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        Goo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "      Sub Goo()" + vbCrLf +
                "            Goo()" + vbCrLf +
                "      End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, False},
                {FormattingOptions2.TabSize, 4},
                {FormattingOptions2.IndentationSize, 6}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ReFormatWithTabsEnabledSmallIndentAndLargeTab() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        Goo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "  Sub Goo()" + vbCrLf +
                vbTab + " Goo()" + vbCrLf +
                "  End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, True},
                {FormattingOptions2.TabSize, 3},
                {FormattingOptions2.IndentationSize, 2}
            }

            Await AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function RegressionCommentFollowsSubsequentIndent4173() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        'comment" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        'comment" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function FormatUsingOverloads() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "Goo()    " + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        Goo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Await AssertFormatUsingAllEntryPointsAsync(code, expected)
        End Function

        <WorkItem(538533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538533")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4173_1() As Task
            Dim code = "Dim a = <xml>   <%=<xml></xml>%>    </xml>"
            Dim expected = "        Dim a = <xml><%= <xml></xml> %></xml>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <WorkItem(538533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538533")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4173_2() As Task
            Dim code = <Code>Class Goo
        Sub Goo()           ' Comment
  Goo()    
    End Sub
End Class</Code>

            Dim expected = <Code>Class Goo
	Sub Goo()           ' Comment
		Goo()
	End Sub
End Class</Code>

            Dim optionSet = New OptionsCollection(LanguageNames.VisualBasic) From
            {
                {FormattingOptions2.UseTabs, True}
            }

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value, optionSet)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function FormatUsingAutoGeneratedCodeOperationProvider() As Task
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "Goo()    " + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "    Sub Goo()" + vbCrLf +
                "        Goo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <WorkItem(538533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538533")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4173_3() As Task
            Dim code = "
Class C
    Sub Goo()
        Dim xml = <xml><code><node></node></code></xml>

        Dim j = From node In xml.<code>
                Select node.@att
    End Sub
End Class"
            Dim expected = "
Class C
    Sub Goo()
        Dim xml = <xml><code><node></node></code></xml>

        Dim j = From node In xml.<code>
                Select node.@att
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <WorkItem(538533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538533")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4173_4() As Task
            Dim code = <code>Class C
            Sub Main(args As String())
Dim r = 2
                    'goo
End Sub
            End Class</code>
            Dim expected = <code>Class C
    Sub Main(args As String())
        Dim r = 2
        'goo
    End Sub
End Class</code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538533")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4173_5() As Task
            Dim code = <code>Module Module1
    Public Sub goo
        ()
    End Sub
End Module</code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(538533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538533")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4173_6() As Task
            Dim code = <code>Module module1
#If True Then
#End If: goo()
End Module</code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(538533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538533")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4173_7() As Task
            Dim code = <code>Module Module1
    Sub Main()
             Dim x = Sub()
                        End Sub : Dim y = Sub()
            End Sub       ' Incorrect indent
        End Sub
End Module</code>

            Dim expected = <code>Module Module1
    Sub Main()
        Dim x = Sub()
                End Sub : Dim y = Sub()
                                  End Sub       ' Incorrect indent
    End Sub
End Module</code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538533")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4173_8() As Task
            Dim code = <code>Module Module1
    Sub Main()
        '       
            
        '   
    
    End Sub
End Module</code>

            Dim expected = <code>Module Module1
    Sub Main()
        '       

        '   

    End Sub
End Module</code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538533, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538533")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4173_9() As Task
            Dim code = <code>Module Module1
    Sub Main()
            #If True Then
    Dim goo as Integer
                        #End If
        End Sub
End Module</code>

            Dim expected = <code>Module Module1
    Sub Main()
#If True Then
        Dim goo as Integer
#End If
    End Sub
End Module</code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538772, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538772")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4482() As Task
            Dim code = <code>_' Public Function GroupBy(Of K, R)( _
_' ByVal key As KeyFunc(Of K), _
_' ByVal selector As SelectorFunc(Of K, QueryableCollection(Of T), R)) _
' As QueryableCollection(Of R)</code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(538754, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538754")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4459() As Task
            Dim code = <Code>Option Strict Off
Module Module1
Sub Main()
End Sub
Dim x As New List(Of Action(Of Integer)) From {Sub(a As Integer)
Dim z As Integer = New Integer
End Sub, Sub() IsNothing(Nothing), Function() As Integer
Dim z As Integer = New Integer
End Function}
End Module
</Code>
            Dim expected = <Code>Option Strict Off
Module Module1
    Sub Main()
    End Sub
    Dim x As New List(Of Action(Of Integer)) From {Sub(a As Integer)
                                                       Dim z As Integer = New Integer
                                                   End Sub, Sub() IsNothing(Nothing), Function() As Integer
                                                                                          Dim z As Integer = New Integer
                                                                                      End Function}
End Module
</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538675, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538675")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4352() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
  Sub Main(args As String())
0: End
  End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
0:      End
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538703, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538703")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4394() As Task
            Await AssertFormatAsync("
''' <summary>
'''
                          ''' </summary>
Module Program
  Sub Main(args As String())
  End Sub
End Module", "
''' <summary>
'''
''' </summary>
Module Program
    Sub Main(args As String())
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function GetTypeTest() As Task
            Dim code = "Dim a = GetType     (       Object          )"
            Dim expected = "        Dim a = GetType(Object)"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function NewObjectTest() As Task
            Dim code = "Dim a = New Object ( )"
            Dim expected = "        Dim a = New Object()"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected), debugMode:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function CTypeTest() As Task
            Dim code = "Dim a = CType       (       args        ,   String  ( )   )     "
            Dim expected = "        Dim a = CType(args, String())"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TernaryConditionTest() As Task
            Dim code = "Dim a = If (    True    , 1, 2)"
            Dim expected = "        Dim a = If(True, 1, 2)"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TryCastTest() As Task
            Dim code = "Dim a = TryCast (       args        , String())"
            Dim expected = "        Dim a = TryCast(args, String())"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <WorkItem(538703, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538703")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4394_1() As Task
            Dim code = "
Class C
    '''<summary>
        '''     Test Method
'''</summary>
    Sub Method()
    End Sub
End Class"
            Dim expected = "
Class C
    '''<summary>
    '''     Test Method
    '''</summary>
    Sub Method()
    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

        <WorkItem(538889, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538889")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4639() As Task
            Dim code = "Imports <xmlns=""http://DefaultNamespace""       >       "
            Dim expected = "Imports <xmlns=""http://DefaultNamespace"">"

            Await AssertFormatAsync(code, expected)
        End Function

        <WorkItem(538891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538891")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4641() As Task
            Dim code = <Code>Module module1
	 Structure C
 
	 End Structure
 
	 Sub goo()
		  Dim cc As C   ?   = New   C   ?   (   )   
	 End Sub
End Module</Code>

            Dim expected = <Code>Module module1
    Structure C

    End Structure

    Sub goo()
        Dim cc As C? = New C?()
    End Sub
End Module</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538892, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538892")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4642() As Task
            Dim code = <Code>_      
    </Code>

            Dim expected = <Code>_
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538894")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4644() As Task
            Dim code = <Code>Option Explicit Off
Module Module1
    Sub Main()
        Dim mmm = Sub(ByRef x As String, _
                                y As Integer)
                      Console.WriteLine(x &amp; y)
                  End Sub, zzz = Sub(y, _
                                            x)
                                     mmm(y, _
                                       x)
                                 End Sub
                                        lll = Sub(x _
                                        )
        Console.WriteLine(x)
                                            End Sub
    End Sub
End Module</Code>

            Dim expected = <Code>Option Explicit Off
Module Module1
    Sub Main()
        Dim mmm = Sub(ByRef x As String, _
                                y As Integer)
                      Console.WriteLine(x &amp; y)
                  End Sub, zzz = Sub(y, _
                                            x)
                                     mmm(y, _
                                       x)
                                 End Sub
        lll = Sub(x _
        )
                  Console.WriteLine(x)
              End Sub
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538897")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4647() As Task
            Dim code = <Code>Option Explicit Off
Module Module1
    Sub Main()
        Dim mmm = Sub(ByRef x As String, _
                                y As Integer)
                      Console.WriteLine(x &amp; y)
                  End Sub, zzz = Sub(y, _
x)
                                     mmm(y, _
                                       x)
                                 End Sub : Dim _
                                     lll = Sub(x _
                                         )
                                               Console.WriteLine(x)
                                           End Sub
    End Sub
End Module
</Code>

            Dim expected = <Code>Option Explicit Off
Module Module1
    Sub Main()
        Dim mmm = Sub(ByRef x As String, _
                                y As Integer)
                      Console.WriteLine(x &amp; y)
                  End Sub, zzz = Sub(y, _
x)
                                     mmm(y, _
                                       x)
                                 End Sub : Dim _
                                     lll = Sub(x _
                                         )
                                               Console.WriteLine(x)
                                           End Sub
    End Sub
End Module
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(538962, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538962")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function WorkItem4737() As Task
            Dim code = "Dim x = <?xml version =""1.0""?><code></code>"
            Dim expected = "        Dim x = <?xml version=""1.0""?><code></code>"

            Await AssertFormatAsync(CreateMethod(code), CreateMethod(expected))
        End Function

        <WorkItem(539031, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539031")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix4826() As Task
            Dim code = <Code>Imports &lt;xmlns:a=""&gt;
Module Program
    Sub Main()
        Dim x As New Dictionary(Of String, XElement) From {{"Root", &lt;x/&gt;}}
        x!Root%.&lt;nodes&gt;...&lt;a:subnodes&gt;.@a:b.ToString$
        With x
            !Root.@a:b.EndsWith("").ToString$()
            With !Root
                .&lt;b&gt;.Value.StartsWith("")
                ...&lt;c&gt;(0).Value.ToString$()
                .ToString()
                Call .ToString()
            End With
        End With
        Dim buffer As New Byte(1023) {}
    End Sub
End Module
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Parentheses() As Task
            Dim code = <Code>Class GenericMethod
    Sub Method(Of T)(t1 As T)
        NewMethod(Of T)(t1)
    End Sub

    Private Shared Sub NewMethod(Of T)(t1 As T)
        Dim a As T
        a = t1
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(539170, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539170")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5022() As Task
            Dim code = <Code>Class A
: _
Dim x
End Class</Code>

            Dim expected = <Code>Class A
    : _
    Dim x
End Class</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539324")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5232() As Task
            Dim code = <Code>Imports System
Module M
    Sub Main()
        If False Then If True Then Else Else Console.WriteLine(1)
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Module M
    Sub Main()
        If False Then If True Then Else Else Console.WriteLine(1)
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539353")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5270() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim q = 2 +                 REM
            3
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim q = 2 +                 REM
            3
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539358, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539358")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5277() As Task
            Dim code = <Code>
#If True Then
               #End If
</Code>

            Dim expected = <Code>
#If True Then
#End If
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539455, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539455")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5432() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        dim q = 2 + _
            'comment
 
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        dim q = 2 + _
        'comment

    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539351")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5268() As Task
            Dim code = <Code>
#If True _
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(539351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539351")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5268_1() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
#If True _

End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(539473, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539473")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5456() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim goo = New With {.goo = "goo",.bar = "bar"}
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim goo = New With {.goo = "goo", .bar = "bar"}
    End Sub
End Module</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539474")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5457() As Task
            Dim code = <Code>Module module1
    Sub main()
        Dim var1 As New Class1
        [|var1.goofoo = 42

    End Sub
    Sub something()
        something()
    End Sub
End Module

Public Class Class1
    Public goofoo As String|]
End Class</Code>

            Dim expected = <Code>Module module1
    Sub main()
        Dim var1 As New Class1
        var1.goofoo = 42

    End Sub
    Sub something()
        something()
    End Sub
End Module

Public Class Class1
    Public goofoo As String
End Class</Code>

            Await AssertFormatSpanAsync(code.Value.Replace(vbLf, vbCrLf), expected.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(539503, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539503")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5492() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim classNum As Integer 'comment
        : classNum += 1 : Dim i As Integer

    End Sub
End Module
 
</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim classNum As Integer 'comment
        : classNum += 1 : Dim i As Integer

    End Sub
End Module

</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539508, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539508")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5497() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
                        : 'comment
 
 
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        : 'comment


    End Sub
End Module</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539581")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5594() As Task
            Dim code = <Code>
&lt;Assembly : MyAttr()&gt;
&lt;Module : MyAttr()&gt;

Module Module1
    Class MyAttr
        Inherits Attribute
    End Class

    Sub main()
    End Sub

End Module
</Code>

            Dim expected = <Code>
&lt;Assembly: MyAttr()&gt;
&lt;Module: MyAttr()&gt;

Module Module1
    Class MyAttr
        Inherits Attribute
    End Class

    Sub main()
    End Sub

End Module
</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539582")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5595() As Task
            Dim code = <Code>
Imports System.Xml
&lt;SomeAttr()&gt; Module Module1
    Sub Main()
    End Sub
End Module
</Code>

            Dim expected = <Code>
Imports System.Xml
&lt;SomeAttr()&gt; Module Module1
    Sub Main()
    End Sub
End Module
</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539616")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5637() As Task
            Dim code = <Code>Public Class Class1
	'this line is comment line
	Sub sub1(ByVal aa As Integer)

    End Sub
End Class</Code>

            Dim expected = <Code>Public Class Class1
    'this line is comment line
    Sub sub1(ByVal aa As Integer)

    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlTest() As Task
            Await AssertFormatAsync("
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim book =                   </book     >
        GetXml(          </book         >)
    End Sub
End Module", "
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim book =                   </book>
        GetXml(          </book>)
    End Sub
End Module")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlDocument() As Task
            Await AssertFormatAsync("
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim book = <?xml version=""1.0""?>
                             <?fff fff?>
    <!-- ffff -->
                 <book/>
      <!-- last comment! yeah :) -->

    End Sub
End Module", "
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim book = <?xml version=""1.0""?>
                   <?fff fff?>
                   <!-- ffff -->
                   <book/>
                   <!-- last comment! yeah :) -->

    End Sub
End Module")
        End Function

        <Fact>
        <WorkItem(539458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539458")>
        <WorkItem(539459, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539459")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlProcessingInstruction() As Task
            Await AssertFormatAsync("
Module Program
    Sub Main()
        Dim x = <?xml version=""1.0""?>
                                         <?blah?>
    <xml></xml>
    End Sub
End Module", "
Module Program
    Sub Main()
        Dim x = <?xml version=""1.0""?>
                <?blah?>
                <xml></xml>
    End Sub
End Module")
        End Function

        <WorkItem(539463, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539463")>
        <WorkItem(530597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530597")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlTest5442() As Task
            Using workspace = New AdhocWorkspace()

                Dim inputOutput = "
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim goo = _
                     <LongBook>
                         <%= _
                             From i In <were><a><b><c></c></b></a></were> _
                             Where i IsNot Nothing _
                             Select _
                                 <f>
                                     <g>
                                         <f>
                                         </f>
                                     </g>
                                 </f> _
                         %>
                     </LongBook>

    End Sub
End Module"

                Dim project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.VisualBasic)
                Dim document = project.AddDocument("Document", SourceText.From(inputOutput))
                Dim root = Await document.GetSyntaxRootAsync()
                Dim options = VisualBasicSyntaxFormattingOptions.Default

                ' format first time
                Dim result = Formatter.GetFormattedTextChanges(root, workspace.Services, options, CancellationToken.None)
                AssertResult(inputOutput, Await document.GetTextAsync(), result)

                Dim document2 = document.WithText((Await document.GetTextAsync()).WithChanges(result))
                Dim root2 = Await document2.GetSyntaxRootAsync()

                ' format second time
                Dim result2 = Formatter.GetFormattedTextChanges(root, workspace.Services, options, CancellationToken.None)
                AssertResult(inputOutput, Await document2.GetTextAsync(), result2)
            End Using
        End Function

        <WorkItem(539687, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539687")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5731() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        If True Then : 'comment
            End If
 
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        If True Then : 'comment
        End If

    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539545, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539545")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5547() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Select Case True
            Case True
            'comment
        End Select
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Select Case True
            Case True
                'comment
        End Select
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539453, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539453")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5430() As Task
            Dim code = "
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim abc = <xml>
                      <video>video1</video>
                  </xml>

        Dim r = From q In abc...<video> _

    End Sub
End Module"

            Await AssertFormatAsync(code, code)
        End Function

        <WorkItem(539889, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539889")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix5989() As Task
            Dim code = <Code>Imports System

Module Program
    Sub Main(args As String())
        Console.WriteLine(CInt (42))
    End Sub
End Module
</Code>

            Dim expected = <Code>Imports System

Module Program
    Sub Main(args As String())
        Console.WriteLine(CInt(42))
    End Sub
End Module
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539409, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539409")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix6367() As Task
            Dim code = <Code>Module Program
    Sub Main(args As String())
        Dim a As Integer = 1'Test
    End Sub
End Module
</Code>

            Dim expected = <Code>Module Program
    Sub Main(args As String())
        Dim a As Integer = 1 'Test
    End Sub
End Module
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(539409, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539409")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix6367_1() As Task
            Dim code = <Code>Module Program
    Sub Main(args As String())
        Dim a As Integer = 1    'Test
    End Sub
End Module
</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(540678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540678")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BugFix7023_1() As Task
            Dim code = <Code>Module Program
    Public Operator +(x As Integer, y As Integer)
        Console.WriteLine("GOO")
    End Operator
End Module
</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlTextWithEmbededExpression1() As Task
            Await AssertFormatAsync("
Class C
    Sub Method()
        Dim q = <xml>
                    test <xml2><%= <xml3><xml4>
                                                          </xml4></xml3>
                    %></xml2>
                </xml>
    End Sub
End Class", "
Class C
    Sub Method()
        Dim q = <xml>
                    test <xml2><%= <xml3><xml4>
                                       </xml4></xml3>
                               %></xml2>
                </xml>
    End Sub
End Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlTextWithEmbededExpression2() As Task
            Await AssertFormatAsync("
Class C2
    Sub Method()
        Dim q = <xml>
                   tst  <%= <xml2><xml3><xml4>
                                                   </xml4></xml3>
                                                   </xml2>
                    %>
                </xml>
    End Sub
End Class", "
Class C2
    Sub Method()
        Dim q = <xml>
                   tst  <%= <xml2><xml3><xml4>
                                </xml4></xml3>
                            </xml2>
                        %>
                </xml>
    End Sub
End Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlText() As Task
            Await AssertFormatAsync("
Class C22
    Sub Method()
        Dim q = <xml>
                   tst   
<xml2><xml3><xml4>
                    </xml4></xml3>
                    </xml2>
                </xml>
    End Sub
End Class", "
Class C22
    Sub Method()
        Dim q = <xml>
                   tst   
<xml2><xml3><xml4>
                        </xml4></xml3>
                    </xml2>
                </xml>
    End Sub
End Class")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlTextWithComment() As Task
            Await AssertFormatAsync("
Class C223
    Sub Method()
        Dim q = <xml>
                    <!-- -->
                   t
        st   
<xml2><xml3><xml4>
                    </xml4></xml3>
                    </xml2>
                </xml>
    End Sub
End Class", "
Class C223
    Sub Method()
        Dim q = <xml>
                    <!-- -->
                   t
        st   
<xml2><xml3><xml4>
                        </xml4></xml3>
                    </xml2>
                </xml>
    End Sub
End Class")
        End Function

        <WorkItem(541628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541628")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function MultipleControlVariables() As Task
            Dim code = <Code>Module Program
    Sub Main(args As String())
        Dim i, j As Integer
        For i = 0 To 1
            For j = 0 To 1
                Next j, i
    End Sub
End Module</Code>

            Dim expected = <Code>Module Program
    Sub Main(args As String())
        Dim i, j As Integer
        For i = 0 To 1
            For j = 0 To 1
        Next j, i
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <WorkItem(541561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541561")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ColonTrivia() As Task
            Dim code = <Code>Module Program
                        Sub Goo3()
                : End Sub

    Sub Main()
    End Sub
End Module</Code>

            Dim expected = <Code>Module Program
    Sub Goo3()
    : End Sub

    Sub Main()
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function MemberAccessInObjectMemberInitializer() As Task
            Dim code = <Code>Module Program
    Sub Main()
        Dim aw = New With {.a = 1, .b = 2+.a}
    End Sub
End Module</Code>

            Dim expected = <Code>Module Program
    Sub Main()
        Dim aw = New With {.a = 1, .b = 2 + .a}
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BinaryConditionalExpression() As Task
            Dim code = <Code>Module Program
    Sub Main()
        Dim x = If(Nothing, "") ' Inline
        x.ToString
    End Sub
End Module</Code>

            Dim expected = <Code>Module Program
    Sub Main()
        Dim x = If(Nothing, "") ' Inline
        x.ToString
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(539574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539574")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function Preprocessors() As Task
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
 
#Const [PUBLIC]    =    3
#Const     goo = 23
#Const goo2=23
#Const goo3 = 23
#Const  goo4  =  23
#Const     goo5    =           23
 
 
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

#Const [PUBLIC] = 3
#Const goo = 23
#Const goo2 = 23
#Const goo3 = 23
#Const goo4 = 23
#Const goo5 = 23


    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(10027, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function RandomCode1() As Task
            Dim code = <Code>'Imports alias 'goo' conflicts with 'goo' declared in the root namespace'</Code>
            Dim expected = <Code>'Imports alias 'goo' conflicts with 'goo' declared in the root namespace'</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(10027, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function RandomCode2() As Task
            Dim code = <Code>'Imports alias 'goo' conflicts with 'goo' declared in the root 
namespace'</Code>
            Dim expected = <Code>'Imports alias 'goo' conflicts with 'goo' declared in the root 
namespace'</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(542698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542698")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ColonTrivia1() As Task
            Dim code = <Code>Imports _
    System.Collections.Generic _
    :
: Imports _
                      System
Imports System.Linq

Module Program
    Sub Main(args As String())
        Console.WriteLine("TEST")
    End Sub
End Module
</Code>

            Dim expected = <Code>Imports _
    System.Collections.Generic _
:
: Imports _
                      System
Imports System.Linq

Module Program
    Sub Main(args As String())
        Console.WriteLine("TEST")
    End Sub
End Module
</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(542698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542698")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ColonTrivia2() As Task
            Dim code = <Code>Imports _
    System.Collections.Generic _
:
: Imports _
                      System
Imports System.Linq

Module Program
    Sub Main(args As String())
        Console.WriteLine("TEST")
    End Sub
End Module
</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <Fact>
        <WorkItem(543197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543197")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function KeyInAnonymousType() As Task
            Dim code = <Code>Class C
    Sub S()
        Dim product = New With {Key.Name = "goo"}
    End Sub
End Class
</Code>

            Dim expected = <Code>Class C
    Sub S()
        Dim product = New With {Key .Name = "goo"}
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(544008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544008")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TestGetXmlNamespace() As Task
            Dim code = <Code>Class C
    Sub S()
        Dim x = GetXmlNamespace(asdf)
    End Sub
End Class
</Code>

            Dim expected = <Code>Class C
    Sub S()
        Dim x = GetXmlNamespace(asdf)
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(539409, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539409")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function StructuredTrivia() As Task
            Dim code = <Code>#const goo=2.0d</Code>
            Dim expected = <Code>#const goo = 2.0d</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function FormatComment1() As Task
            Dim code = <Code>Class A
    Sub Test()
         Console.WriteLine()
         :
             ' test

         Console.WriteLine()
    End Sub
End Class</Code>

            Dim expected = <Code>Class A
    Sub Test()
        Console.WriteLine()
        :
        ' test

        Console.WriteLine()
    End Sub
End Class</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function FormatComment2() As Task
            Dim code = <Code>Class A
    Sub Test()
         Console.WriteLine()
         
             ' test

         Console.WriteLine()
    End Sub
End Class</Code>

            Dim expected = <Code>Class A
    Sub Test()
        Console.WriteLine()

        ' test

        Console.WriteLine()
    End Sub
End Class</Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(543248, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543248")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function FormatBadCode() As Task
            Dim code = <Code>Imports System
Class Program
    Shared Sub Main(args As String())
        SyncLock From y As Char  i, j As Char In String.Empty

        End SyncLock
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <Fact>
        <WorkItem(544496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544496")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function AttributeInParameterList() As Task
            Dim code = <Code>Module Program
    Sub Main(           &lt;Description&gt; args As String())
    End Sub
End Module</Code>

            Dim expected = <Code>Module Program
    Sub Main(&lt;Description&gt; args As String())
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(544980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544980")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlElement_Expression() As Task
            Dim code = <Code>Module Program
    Dim x = &lt;x       &lt;%=      "" %&gt;        /&gt;
End Module</Code>

            Dim expected = <Code>Module Program
    Dim x = &lt;x &lt;%= "" %&gt;/&gt;
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(542976, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542976")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlElementStartTag() As Task
            Dim code = <Code>Module Program
    Dim x = &lt;code
            &gt;
            &lt;/code&gt;
End Module</Code>

            Dim expected = <Code>Module Program
    Dim x = &lt;code
                &gt;
            &lt;/code&gt;
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(545088, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545088")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ForNext_MultipleVariables() As Task
            Dim code = <Code>Module Program
    Sub Method()
For a = 0 To 1
    For b = 0 To 1
        For c = 0 To 1
        Next c, b, a
    End Sub
End Module</Code>

            Dim expected = <Code>Module Program
    Sub Method()
        For a = 0 To 1
            For b = 0 To 1
                For c = 0 To 1
        Next c, b, a
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(545088, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545088")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ForNext_MultipleVariables2() As Task
            Dim code = <Code>Module Program
    Sub Method()
For z = 0 To 1
For a = 0 To 1
    For b = 0 To 1
        For c = 0 To 1
        Next c, b, a
            Next z
    End Sub
End Module</Code>

            Dim expected = <Code>Module Program
    Sub Method()
        For z = 0 To 1
            For a = 0 To 1
                For b = 0 To 1
                    For c = 0 To 1
            Next c, b, a
        Next z
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(544459, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544459")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function DictionaryAccessOperator() As Task
            Dim code = <Code>Class S
    Default Property Def(s As String) As String
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property
    Property Y As String
End Class
Module Program
    Sub Main(args As String())
        Dim c As New S With {.Y =!Hello}
    End Sub
End Module</Code>

            Dim expected = <Code>Class S
    Default Property Def(s As String) As String
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property
    Property Y As String
End Class
Module Program
    Sub Main(args As String())
        Dim c As New S With {.Y = !Hello}
    End Sub
End Module</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact>
        <WorkItem(542976, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542976")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XmlElementStartTag1() As Task
            Await AssertFormatAsync("
Class C
	Sub Method()
		Dim book = <book
                   version=""goo""
                   >
                   </book>
	End Sub
End Class", "
Class C
    Sub Method()
        Dim book = <book
                       version=""goo""
                       >
                   </book>
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ElasticNewlines()
            Dim text = <text>Class C
Implements INotifyPropertyChanged
Dim _p As Integer
Property P As Integer
    Get
        Return 0
    End Get

    Set(value As Integer)
        SetProperty(_p, value, "P")
    End Set
End Property
Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
    If Not EqualityComparer(Of T).Default.Equals(field, value) Then
        field = value
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
    End If
End Sub
End Class</text>.Value.Replace(vbLf, vbCrLf)

            Dim expected = <text>Class C
    Implements INotifyPropertyChanged

    Dim _p As Integer

    Property P As Integer
        Get
            Return 0
        End Get

        Set(value As Integer)
            SetProperty(_p, value, "P")
        End Set
    End Property

    Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
        If Not EqualityComparer(Of T).Default.Equals(field, value) Then
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End If
    End Sub
End Class</text>.Value.Replace(vbLf, vbCrLf)
            Dim root = SyntaxFactory.ParseCompilationUnit(text)

            Dim goo As New SyntaxAnnotation()
            Dim implementsStatement = DirectCast(root.Members(0), ClassBlockSyntax).Implements.First()
            root = root.ReplaceNode(implementsStatement, implementsStatement.NormalizeWhitespace(elasticTrivia:=True).WithAdditionalAnnotations(goo))

            Dim field = DirectCast(root.Members(0), ClassBlockSyntax).Members(0)
            root = root.ReplaceNode(field, field.NormalizeWhitespace(elasticTrivia:=True).WithAdditionalAnnotations(goo))
            Dim prop = DirectCast(root.Members(0), ClassBlockSyntax).Members(1)
            root = root.ReplaceNode(prop, prop.NormalizeWhitespace(elasticTrivia:=True).WithAdditionalAnnotations(goo))

            Dim method = DirectCast(root.Members(0), ClassBlockSyntax).Members(2)
            root = root.ReplaceNode(method, method.NormalizeWhitespace(elasticTrivia:=True).WithAdditionalAnnotations(goo))

            Using workspace = New AdhocWorkspace()
                Dim result = Formatter.Format(root, goo, workspace.Services, VisualBasicSyntaxFormattingOptions.Default, CancellationToken.None).ToString()
                Assert.Equal(expected, result)
            End Using
        End Sub

        <Fact>
        <WorkItem(545630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545630")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function SpacesInXmlStrings() As Task
            Dim text = "Imports <xmlns:x='&#70;'>"

            Await AssertFormatAsync(text, text)
        End Function

        <Fact>
        <WorkItem(545680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545680")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function SpaceBetweenPercentGreaterThanAndXmlName() As Task
            Dim text = <code>Module Program
    Sub Main(args As String())
    End Sub
    Public Function GetXml() As XElement
        Return &lt;field name=&lt;%= 1 %&gt; type=&lt;%= 2 %&gt;&gt;&lt;/field&gt;
    End Function
End Module</code>

            Await AssertFormatLf2CrLfAsync(text.Value, text.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function SpacingAroundXmlEntityLiterals() As Task
            Dim code =
<Code><![CDATA[Class C
    Sub Bar()
        Dim goo = <Code>&lt;&gt;</Code>
    End Sub
End Class
]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <WorkItem(547005, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547005")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function BadDirectivesAreValidRanges() As Task
            Dim code = <Code>
#If False Then
#end  Ｒｅｇｉｏｎ
#End If

#If False Then
#end
#End If
</Code>

            Dim expected = <Code>
#If False Then
#end  Ｒｅｇｉｏｎ
#End If

#If False Then
#end
#End If
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(17313, "DevDiv_Projects/Roslyn")>
        Public Async Function TestElseIfFormatting_Directive() As Task
            Dim code =
<Code><![CDATA[
#If True Then
#Else If False Then
#End If
]]></Code>

            Dim expected =
<Code><![CDATA[
#If True Then
#ElseIf False Then
#End If
]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(529899, "DevDiv_Projects/Roslyn")>
        Public Async Function IndentContinuedLineOfSingleLineLambdaToFunctionKeyword() As Task
            Dim code =
<Code><![CDATA[
Module Program
    Sub Main(ByVal args As String())
        Dim a1 = Function() args(0) _
                    + 1
    End Sub
End Module
]]></Code>

            Dim expected =
<Code><![CDATA[
Module Program
    Sub Main(ByVal args As String())
        Dim a1 = Function() args(0) _
                    + 1
    End Sub
End Module
]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(604032, "DevDiv_Projects/Roslyn")>
        Public Async Function TestSpaceBetweenEqualsAndDotOfXml() As Task
            Dim code =
<Code><![CDATA[
Module Program
    Sub Main(args As String())
        Dim xml = <Order id="1">
                      <Customer>
                          <Name>Bob</Name>
                      </Customer>
                      <Contents>
                          <Item productId="1" quantity="2"/>
                          <Item productId="2" quantity="1"/>
                      </Contents>
                  </Order>
        With xml
            Dim customerName =.<Customer>.<Name>.Value
            Dim itemCount =...<Item>.Count
            Dim orderId =.@id
        End With
    End Sub
End Module
]]></Code>

            Dim expected =
<Code><![CDATA[
Module Program
    Sub Main(args As String())
        Dim xml = <Order id="1">
                      <Customer>
                          <Name>Bob</Name>
                      </Customer>
                      <Contents>
                          <Item productId="1" quantity="2"/>
                          <Item productId="2" quantity="1"/>
                      </Contents>
                  </Order>
        With xml
            Dim customerName = .<Customer>.<Name>.Value
            Dim itemCount = ...<Item>.Count
            Dim orderId = .@id
        End With
    End Sub
End Module
]]></Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(604092, "DevDiv_Projects/Roslyn")>
        Public Async Function AnchorIndentToTheFirstTokenOfXmlBlock() As Task
            Dim code =
<Code><![CDATA[
Module Program
    Sub Main(args As String())
        With <a>
             </a>
						Dim s = 1
        End With
        SyncLock <b>
                 </b>
                     Return
        End SyncLock
        Using <c>
              </c>
                  Return
        End Using
        For Each reallyReallyReallyLongIdentifierNameHere In <d>
                                                             </d>
                                                                 Return
        Next
    End Sub
End Module
]]></Code>

            Dim expected =
<Code><![CDATA[
Module Program
    Sub Main(args As String())
        With <a>
             </a>
            Dim s = 1
        End With
        SyncLock <b>
                 </b>
            Return
        End SyncLock
        Using <c>
              </c>
            Return
        End Using
        For Each reallyReallyReallyLongIdentifierNameHere In <d>
                                                             </d>
            Return
        Next
    End Sub
End Module
]]></Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(530366, "DevDiv_Projects/Roslyn")>
        Public Async Function ForcedSpaceBetweenXmlNameTokenAndPercentGreaterThanToken() As Task
            Dim code =
<Code><![CDATA[
Module Module1
    Sub Main()
        Dim e As XElement
        Dim y = <root><%= e.@test %></root>
    End Sub
End Module
]]></Code>

            Dim expected =
<Code><![CDATA[
Module Module1
    Sub Main()
        Dim e As XElement
        Dim y = <root><%= e.@test %></root>
    End Sub
End Module
]]></Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(531444, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531444")>
        Public Async Function TestElseIfFormattingForNestedSingleLineIf() As Task
            Dim code =
<Code><![CDATA[
        If True Then Console.WriteLine(1) Else If True Then Return
]]></Code>

            Dim actual = CreateMethod(code.Value)

            ' Verify "Else If" doesn't get formatted to "ElseIf"
            Await AssertFormatLf2CrLfAsync(actual, actual)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TestDontCrashOnMissingTokenWithComment() As Task
            Dim code =
<Code><![CDATA[
Namespace NS
    Class CL
        Sub Method()
            Dim goo = Sub(x) 'Comment
]]></Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TestBang() As Task
            Dim code =
<Code><![CDATA[
Imports System.Collections
 
Module Program
    Sub Main()
        Dim x As New Hashtable
        Dim y = x               !                    _
        Goo
    End Sub
End Module
]]></Code>

            Dim expected =
<Code><![CDATA[
Imports System.Collections

Module Program
    Sub Main()
        Dim x As New Hashtable
        Dim y = x ! _
        Goo
    End Sub
End Module
]]></Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(679864, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/679864")>
        Public Async Function InsertSpaceBetweenXMLMemberAttributeAccessAndEqualsToken() As Task
            Dim expected =
<Code><![CDATA[
Imports System
Imports System.Collections

Module Program
    Sub Main(args As String())
        Dim element = <element></element>
        Dim goo = element.Single(Function(e) e.@Id = 1)
    End Sub
End Module
]]></Code>

            Dim code =
<Code><![CDATA[
Imports System
Imports System.Collections

Module Program
    Sub Main(args As String())
        Dim element = <element></element>
        Dim goo = element.Single(Function(e) e.@Id     =    1)
    End Sub
End Module
]]></Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(923172, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/923172")>
        Public Async Function TestMemberAccessAfterOpenParen() As Task
            Dim expected =
<Code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        With args
            If .IsNew = True Then
                Return Nothing
            Else
                Return CTypeDynamic(Of T)(.Value, .Hello)
            End If
        End With
    End Sub
End Module
]]></Code>

            Dim code =
<Code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        With args
            If .IsNew = True Then
                Return Nothing
            Else
                Return CTypeDynamic(Of T)(  .Value,              .Hello)
            End If
        End With
    End Sub
End Module
]]></Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(923180, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/923180")>
        Public Async Function TestXmlMemberAccessDot() As Task
            Dim expected =
<Code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        With x.<Service>.First
            If .<WorkerServiceType>.Count > 0 Then
                Main(.<A>.Value, .<B>.Value)
                Dim i = .<A>.Value + .<B>.Value
            End If
        End With
    End Sub
End Module
]]></Code>

            Dim code =
<Code><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        With x.<Service>.First
            If.<WorkerServiceType>.Count > 0 Then
                Main(.<A>.Value,.<B>.Value)
                Dim i = .<A>.Value +.<B>.Value
            End If
        End With
    End Sub
End Module
]]></Code>
            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(530601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530601")>
        Public Async Function TestElasticFormattingPropertySetter() As Task
            Dim parameterList = SyntaxFactory.ParseParameterList(String.Format("(value As {0})", "Integer"))
            Dim setter = SyntaxFactory.AccessorBlock(SyntaxKind.SetAccessorBlock,
                                                   SyntaxFactory.AccessorStatement(SyntaxKind.SetAccessorStatement, SyntaxFactory.Token(SyntaxKind.SetKeyword)).
                                                                 WithParameterList(parameterList),
                                                   SyntaxFactory.EndBlockStatement(SyntaxKind.EndSetStatement, SyntaxFactory.Token(SyntaxKind.SetKeyword)))
            Dim setPropertyStatement = SyntaxFactory.ParseExecutableStatement(String.Format("SetProperty({0}, value, ""{1}"")", "field", "Property")).WithLeadingTrivia(SyntaxFactory.ElasticMarker)
            setter = setter.WithStatements(SyntaxFactory.SingletonList(setPropertyStatement))

            Dim solution = New AdhocWorkspace().CurrentSolution
            Dim project = solution.AddProject("proj", "proj", LanguageNames.VisualBasic)
            Dim document = project.AddDocument("goo.vb", <text>Class C
    WriteOnly Property Prop As Integer
    End Property
End Class</text>.Value)

            Dim propertyBlock = (Await document.GetSyntaxRootAsync()).DescendantNodes().OfType(Of PropertyBlockSyntax).Single()
            document = Await Formatter.FormatAsync(document.WithSyntaxRoot(
                (Await document.GetSyntaxRootAsync()).ReplaceNode(propertyBlock, propertyBlock.WithAccessors(SyntaxFactory.SingletonList(setter)))))

            Dim actual = (Await document.GetTextAsync()).ToString()
            Assert.Equal(actual, actual)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TestWarningDirectives() As Task
            Dim text = <Code>
                           #  enable           warning[BC000],bc123,             ap456,_789'          comment
Module Program
        #   disable     warning   'Comment
    Sub Main()
        #disable       warning          bc123,            bC456,someId789
    End Sub
End Module
        #   enable     warning    
</Code>

            Dim expected = <Code>
#enable warning [BC000], bc123, ap456, _789'          comment
Module Program
#disable warning   'Comment
    Sub Main()
#disable warning bc123, bC456, someId789
    End Sub
End Module
#enable warning
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TestIncompleteWarningDirectives() As Task
            Dim text = <Code>
#   disable
Module M1
        #   enable     warning[bc123],   ' Comment   
End Module
</Code>

            Dim expected = <Code>
#disable
Module M1
#enable warning [bc123],   ' Comment   
End Module
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <WorkItem(796562, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/796562")>
        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function TriviaAtEndOfCaseBelongsToNextCase() As Task
            Dim text = <Code>
Class X
    Function F(x As Integer) As Integer
        Select Case x
            Case 1
                Return 2
                ' This comment describes case 1
            ' This comment describes case 2
            Case 2,
                Return 3
        End Select

        Return 5
    End Function
End Class
</Code>

            Dim expected = <Code>
Class X
    Function F(x As Integer) As Integer
        Select Case x
            Case 1
                Return 2
                ' This comment describes case 1
            ' This comment describes case 2
            Case 2,
                Return 3
        End Select

        Return 5
    End Function
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <WorkItem(938188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/938188")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function XelementAttributeSpacing() As Task
            Dim text = <Code>
Class X
    Function F(x As Integer) As Integer
        Dim x As XElement
        x.@Goo= "Hello"
    End Function
End Class
</Code>

            Dim expected = <Code>
Class X
    Function F(x As Integer) As Integer
        Dim x As XElement
        x.@Goo = "Hello"
    End Function
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ConditionalAccessFormatting() As Task
            Const code = "
Module Module1
    Class G
        Public t As String
    End Class

    Sub Main()
        Dim x = New G()
        Dim q = x ? . t ? ( 0 )
        Dim me = Me ? . ToString()
        Dim mb = MyBase ? . ToString()
        Dim mc = MyClass ? . ToString()
        Dim i = New With {.a = 3} ? . ToString()
        Dim s = ""Test"" ? . ToString()
        Dim s2 = $""Test"" ? . ToString()
        Dim x1 = <a></a> ? . <b>
        Dim x2 = <a/> ? . <b>
    End Sub
End Module
"

            Const expected = "
Module Module1
    Class G
        Public t As String
    End Class

    Sub Main()
        Dim x = New G()
        Dim q = x?.t?(0)
        Dim me = Me?.ToString()
        Dim mb = MyBase?.ToString()
        Dim mc = MyClass?.ToString()
        Dim i = New With {.a = 3}?.ToString()
        Dim s = ""Test""?.ToString()
        Dim s2 = $""Test""?.ToString()
        Dim x1 = <a></a>?.<b>
        Dim x2 = <a/>?.<b>
    End Sub
End Module
"

            Await AssertFormatAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function ChainedConditionalAccessFormatting() As Task
            Const code = "
Module Module1
    Class G
        Public t As String
    End Class

    Sub Main()
        Dim x = New G()
        Dim q = x ? . t ? . ToString() ? . ToString ( 0 )
        Dim me = Me ? . ToString() ? . Length
        Dim mb = MyBase ? . ToString() ? . Length
        Dim mc = MyClass ? . ToString() ? . Length
        Dim i = New With {.a = 3} ? . ToString() ? . Length
        Dim s = ""Test"" ? . ToString() ? . Length
        Dim s2 = $""Test"" ? . ToString() ? . Length
        Dim x1 = <a></a> ? . <b> ? . <c>
        Dim x2 = <a/> ? . <b> ? . <c>
    End Sub
End Module
"

            Const expected = "
Module Module1
    Class G
        Public t As String
    End Class

    Sub Main()
        Dim x = New G()
        Dim q = x?.t?.ToString()?.ToString(0)
        Dim me = Me?.ToString()?.Length
        Dim mb = MyBase?.ToString()?.Length
        Dim mc = MyClass?.ToString()?.Length
        Dim i = New With {.a = 3}?.ToString()?.Length
        Dim s = ""Test""?.ToString()?.Length
        Dim s2 = $""Test""?.ToString()?.Length
        Dim x1 = <a></a>?.<b>?.<c>
        Dim x2 = <a/>?.<b>?.<c>
    End Sub
End Module
"

            Await AssertFormatAsync(code, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function NameOfFormatting() As Task
            Dim text = <Code>
Module M
    Dim s = NameOf ( M )
End Module
</Code>

            Dim expected = <Code>
Module M
    Dim s = NameOf(M)
End Module
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function InterpolatedString1() As Task
            Dim text = <Code>
Class C
    Sub M()
        Dim a = "World"
        Dim b =$"Hello,  {a}"
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub M()
        Dim a = "World"
        Dim b = $"Hello,  {a}"
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function InterpolatedString2() As Task
            Dim text = <Code>
Class C
    Sub M()
        Dim a = "Hello"
        Dim b = "World"
        Dim c = $"{a}, {b}"
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub M()
        Dim a = "Hello"
        Dim b = "World"
        Dim c = $"{a}, {b}"
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function InterpolatedString3() As Task
            Dim text = <Code>
Class C
    Sub M()
        Dim a = "World"
        Dim b = $"Hello, { a }"
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub M()
        Dim a = "World"
        Dim b = $"Hello, { a }"
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function InterpolatedString4() As Task
            Dim text = <Code>
Class C
    Sub M()
        Dim a = "Hello"
        Dim b = "World"
        Dim c = $"{ a }, { b }"
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub M()
        Dim a = "Hello"
        Dim b = "World"
        Dim c = $"{ a }, { b }"
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function InterpolatedString5() As Task
            Dim text = <Code>
Class C
    Sub M()
        Dim s = $"{42 , -4 :x}"
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub M()
        Dim s = $"{42,-4:x}"
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function CaseCommentsRemainsUndisturbed() As Task
            Dim text = <Code>
Class Program
    Sub Main(args As String())
        Dim s = 0
        Select Case s
            Case 0
            ' Comment should not be indented
            Case 2
                ' comment
                Console.WriteLine(s)
            Case 4
        End Select
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class Program
    Sub Main(args As String())
        Dim s = 0
        Select Case s
            Case 0
            ' Comment should not be indented
            Case 2
                ' comment
                Console.WriteLine(s)
            Case 4
        End Select
    End Sub
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(text.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub NewLineOption_LineFeedOnly()
            Using workspace = New AdhocWorkspace()
                Dim tree = SyntaxFactory.ParseCompilationUnit("Class C" & vbCrLf & "End Class")

                ' replace all EOL trivia with elastic markers to force the formatter to add EOL back
                tree = tree.ReplaceTrivia(tree.DescendantTrivia().Where(Function(tr) tr.IsKind(SyntaxKind.EndOfLineTrivia)), Function(o, r) SyntaxFactory.ElasticMarker)

                Dim options = SyntaxFormattingOptions.Create(
                    workspace.Options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.VisualBasic, vbLf),
                    workspace.Services,
                    tree.Language)

                Dim formatted = Formatter.Format(tree, workspace.Services, options, CancellationToken.None)
                Dim actual = formatted.ToFullString()

                Dim expected = "Class C" & vbLf & "End Class"

                Assert.Equal(expected, actual)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(2822, "https://github.com/dotnet/roslyn/issues/2822")>
        Public Async Function FormatLabelFollowedByDotExpression() As Task
            Dim code = <Code>
Module Module1
    Sub Main()
        With New List(Of Integer)
lab: .Capacity = 15
        End With
    End Sub
End Module
</Code>

            Dim expected = <Code>
Module Module1
    Sub Main()
        With New List(Of Integer)
lab:        .Capacity = 15
        End With
    End Sub
End Module
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(21923, "https://github.com/dotnet/roslyn/issues/21923")>
        Public Async Function FormatNullableTuple() As Task
            Dim code = <Code>
Class C
    Dim x As (Integer, Integer)  ?
End Class
</Code>

            Dim expected = <Code>
Class C
    Dim x As (Integer, Integer)?
End Class
</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(2822, "https://github.com/dotnet/roslyn/issues/2822")>
        Public Async Function FormatOmittedArgument() As Task
            Dim code = <Code>
Class C
    Sub M()
        Call M(
            a,
                    ,
            a
            )
    End Sub
End Class</Code>

            Await AssertFormatLf2CrLfAsync(code.Value, code.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(8258, "https://github.com/dotnet/roslyn/issues/8258")>
        Public Async Function FormatDictionaryOperatorProperly() As Task
            Dim code = "
Class C
    Public Shared Sub AutoFormatSample()

        ' Automatic Code Formatting in VB.NET
        ' Problem with ! dictionary access operator

        Dim dict As New Dictionary(Of String, String)
        dict.Add(""Apple"", ""Green"")
        dict.Add(""Orange"", ""Orange"")
        dict.Add(""Banana"", ""Yellow"")

        Dim x As String = (New Dictionary(Of String, String)(dict)) !Banana 'added space in front of ""!""

        With dict

            !Apple = ""Red""
            Dim multiColors = !Apple &!Orange &!Banana 'missing space between ""&"" and ""!""
            If!Banana = ""Yellow"" Then 'missing space between ""If"" and ""!""
                !Banana = ""Green""
            End If

        End With

    End Sub
End Class"

            Dim expected = "
Class C
    Public Shared Sub AutoFormatSample()

        ' Automatic Code Formatting in VB.NET
        ' Problem with ! dictionary access operator

        Dim dict As New Dictionary(Of String, String)
        dict.Add(""Apple"", ""Green"")
        dict.Add(""Orange"", ""Orange"")
        dict.Add(""Banana"", ""Yellow"")

        Dim x As String = (New Dictionary(Of String, String)(dict))!Banana 'added space in front of ""!""

        With dict

            !Apple = ""Red""
            Dim multiColors = !Apple & !Orange & !Banana 'missing space between ""&"" and ""!""
            If !Banana = ""Yellow"" Then 'missing space between ""If"" and ""!""
                !Banana = ""Green""
            End If

        End With

    End Sub
End Class"

            Await AssertFormatAsync(code, expected)
        End Function

    End Class
End Namespace
