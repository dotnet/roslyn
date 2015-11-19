' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Formatting
    Public Class FormattingTest
        Inherits VisualBasicFormattingTestBase

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Format1()
            Dim code = <Code>                   
    Namespace                   A                               
            End                 Namespace                           </Code>

            Dim expected = <Code>
Namespace A
End Namespace</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub NamespaceBlock()
            Dim code = <Code>                           Namespace                   A                               
    Class            C
                End             Class
            End                 Namespace                           </Code>

            Dim expected = <Code>Namespace A
    Class C
    End Class
End Namespace</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TypeBlock()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub MethodBlock()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub StructBlock()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub EnumBlock()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ModuleBlock()
            Dim code = <Code>        Module module1     
Sub   foo()        
 End      Sub  
                End                 Module        </Code>

            Dim expected = <Code>Module module1
    Sub foo()
    End Sub
End Module</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub InterfaceBlock()
            Dim code = <Code>      Interface        IFoo  
    Sub   foo()  
  End Interface    
</Code>

            Dim expected = <Code>Interface IFoo
    Sub foo()
End Interface
</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub PropertyBlock()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub EventBlock()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WhileBlockNode()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub UsingBlockNode()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub SyncLockBlockNode()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WithBlockNode()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub IfBlockNode()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TryCatchBlockNode()
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
  foo()
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
            foo()
        End Try
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub SelectBlockNode()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub DoLoopBlockNode()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub DoUntilBlockNode()
            Dim code = <Code>Class C
    Sub  Method()    
 Do  Until    False  
   foo()    
  Loop  
    End    Sub   
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        Do Until False
            foo()
        Loop
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ForBlockNode()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub AnchorStatement()
            Dim code = <Code>Imports System
                        Imports System.
                                    Collections.
                                        Generic</Code>

            Dim expected = <Code>Imports System
Imports System.
            Collections.
                Generic</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub AnchorQueryStatement()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub AlignQueryStatement()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Operators1()
            Dim code = <Code>Class C
    Sub Method()
        Dim a = -           1
        Dim a2 = 1-1
        Dim a3 = +          1
        Dim a4 = 1+1
        Dim a5 = 2+(3*-2)
        Foo(2,(3))
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
        Foo(2, (3))
    End Sub
End Class
</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Operators2()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Operators3()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Punctuation()
            Dim code = <Code>   &lt;   Fact    (   )   , Trait (   Traits  .   Feature ,     Traits    .   Features    .   Formatting  )   &gt;    
Class A
End Class</Code>

            Dim expected = <Code>&lt;Fact(), Trait(Traits.Feature, Traits.Features.Formatting)&gt;
Class A
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Punctuation2()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Punctuation3()
            Dim code = <Code><![CDATA[Class C
    <Attribute(foo := "value")>
    Sub Method()
    End Sub
End Class]]></Code>

            Dim expected = <Code><![CDATA[Class C
    <Attribute(foo:="value")>
    Sub Method()
    End Sub
End Class]]></Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Lambda1()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Lambda2()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Lambda3()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Lambda4()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub LineContinuation1()
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    Dim a = 1 + _
                            2 + _
                            3
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim a = 1 + _
                2 + _
                3
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub LineContinuation2()
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    Dim aa = 1 + _
                             2 + _
                             3
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim aa = 1 + _
                 2 + _
                 3
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub LineContinuation3()
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    Dim aa = 1 + _
    2 + _
    3
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim aa = 1 + _
2 + _
3
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub LineContinuation4()
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
                    Dim aa = 1 + _
            _
                                          _
                                     _
    _
            _
    2 + _
    3
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim aa = 1 + _
 _
 _
 _
 _
 _
2 + _
3
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub AnonType()
            Dim code = <Code>Class Foo
    Sub FooMethod()
                Dim SomeAnonType = New With {
                    .foo = "foo",
                 .answer = 42
                                }
    End Sub
End Class</Code>

            Dim expected = <Code>Class Foo
    Sub FooMethod()
        Dim SomeAnonType = New With {
            .foo = "foo",
         .answer = 42
                        }
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub CollectionInitializer()
            Dim code = <Code>Class Foo
    Sub FooMethod()
        Dim somelist = New List(Of Integer) From {
            1,
                2,
        3
                        }
    End Sub
End Class</Code>

            Dim expected = <Code>Class Foo
    Sub FooMethod()
        Dim somelist = New List(Of Integer) From {
            1,
                2,
        3
                        }
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Label1()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Label2()
            Dim code = <Code>Class C
    Sub Method()
GoTo foofoofoofoofoo
        foofoofoofoofoo:        Stop
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method()
        GoTo foofoofoofoofoo
foofoofoofoofoo: Stop
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Label3()
            Dim code = <Code>Class C
    Sub Foo()
        foo()
        x : foo()
        y :
        foo()
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Foo()
        foo()
x:      foo()
y:
        foo()
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Trivia1()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Trivia2()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Trivia3()
            Dim code = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
Dim a =             _
                _
                        _
        1
    End Sub
End Class</Code>

            Dim expected = <Code>Class C
    Sub Method(Optional ByVal i As Integer = 1)
        Dim a = _
 _
 _
                1
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538354, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix3939()
            Dim code = <Code>               
                        Imports System.
                                    Collections.    
                                        Generic             </Code>

            Dim expected = <Code>
Imports System.
            Collections.
                Generic</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538579, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4235()
            Dim code = <Code>               
                #If False Then
                #End If
                    </Code>

            Dim expected = <Code>
#If False Then
#End If
</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals1()
            Dim code = "Dim xml =       <           XML         >               </         XML      >           "
            Dim expected = "        Dim xml = <XML></XML>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals2()
            Dim code = "Dim xml =       <           XML         >    <%=            a            %>           </         XML      >           "
            Dim expected = "        Dim xml = <XML><%= a %></XML>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals3()
            Dim code = "Dim xml =       <           local       :           XML         >               </         local            :           XML      >           "
            Dim expected = "        Dim xml = <local:XML></local:XML>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals4()
            Dim code = "Dim xml =       <           local       :<%= hello %>         >               </               >           "
            Dim expected = "        Dim xml = <local:<%= hello %>></>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals5()
            Dim code = "Dim xml =       <           <%= hello %>         >               </               >           "
            Dim expected = "        Dim xml = <<%= hello %>></>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals6()
            Dim code = "Dim xml =       <           <%= hello %>         /> "
            Dim expected = "        Dim xml = <<%= hello %>/>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals7()
            Dim code = "Dim xml =       <           xml            attr           =           ""1""           /> "
            Dim expected = "        Dim xml = <xml attr=""1""/>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals8()
            Dim code = "Dim xml =       <           xml            attr           =           '1'           /> "
            Dim expected = "        Dim xml = <xml attr='1'/>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals9()
            Dim code = "Dim xml =       <           xml            attr           =           '1'       attr2 = ""2""           attr3       =               <%=             hello               %>      /> "
            Dim expected = "        Dim xml = <xml attr='1' attr2=""2"" attr3=<%= hello %>/>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals10()
            Dim code = "Dim xml =       <           xml            local:attr           =           '1'       attr2 = ""2""           attr3       =               <%=             hello               %>      /> "
            Dim expected = "        Dim xml = <xml local:attr='1' attr2=""2"" attr3=<%= hello %>/>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals11()
            Dim code = "Dim xml =       <           xml            local:attr           =           '1'       <%= attr2 %>    = ""2""           local:<%= attr3 %>      =               <%=             hello               %>      /> "
            Dim expected = "        Dim xml = <xml local:attr='1' <%= attr2 %>=""2"" local:<%= attr3 %>=<%= hello %>/>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals12()
            Dim code = "Dim xml =       <           xml>    test        </xml  > "
            Dim expected = "        Dim xml = <xml>    test        </xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals13()
            Dim code = "Dim xml =       <           xml>    test                <%=         test            %>   </xml  > "
            Dim expected = "        Dim xml = <xml>    test                <%= test %></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals14()
            Dim code = "Dim xml =       <           xml>    test                <%=         test            %>   <%=         test2            %>   </xml  > "
            Dim expected = "        Dim xml = <xml>    test                <%= test %><%= test2 %></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals15()
            Dim code = "Dim xml =       <           xml>   <%=         test1            %>    test                <%=         test            %>   <%=         test2            %>   </xml  > "
            Dim expected = "        Dim xml = <xml><%= test1 %>    test                <%= test %><%= test2 %></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals16()
            Dim code = "Dim xml =       <           xml>    <!--        test            -->         </xml  > "
            Dim expected = "        Dim xml = <xml><!--        test            --></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals17()
            Dim code = "Dim         xml         =           <xml>     <test/>                   <!-- test -->    test            <!-- test -->              </xml>          "
            Dim expected = "        Dim xml = <xml><test/><!-- test -->    test            <!-- test --></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals18()
            Dim code = "Dim         xml         =           <!-- test -->"
            Dim expected = "        Dim xml = <!-- test -->"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals19()
            Dim code = "Dim         xml         =           <?xml-stylesheet            type = ""text/xsl""  href  =   ""show_book.xsl""?>"
            Dim expected = "        Dim xml = <?xml-stylesheet type = ""text/xsl""  href  =   ""show_book.xsl""?>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals20()
            Dim code = "Dim         xml         =           <xml>     <test/>                   <!-- test -->    test            <!-- test -->      <?xml-stylesheet            type = 'text/xsl'  href  =   show_book.xsl?>        </xml>          "
            Dim expected = "        Dim xml = <xml><test/><!-- test -->    test            <!-- test --><?xml-stylesheet type = 'text/xsl'  href  =   show_book.xsl?></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals21()
            Dim code = "Dim         xml         =           <xml>     <test/>                   <!-- test -->    test            <!-- test --> test 2 <?xml-stylesheet            type = 'text/xsl'  href  =   show_book.xsl?>        </xml>          "
            Dim expected = "        Dim xml = <xml><test/><!-- test -->    test            <!-- test --> test 2 <?xml-stylesheet type = 'text/xsl'  href  =   show_book.xsl?></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals22()
            Dim code = "Dim         xml         =           <![CDATA[       Can contain literal <XML> tags      ]]>            "
            Dim expected = "        Dim xml = <![CDATA[       Can contain literal <XML> tags      ]]>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals23()
            Dim code = "Dim         xml         =           <xml>     <test/>                   <!-- test -->    test    <![CDATA[       Can contain literal <XML> tags      ]]>        <!-- test --> test 2 <?xml-stylesheet            type = 'text/xsl'  href  =   show_book.xsl?>        </xml>          "
            Dim expected = "        Dim xml = <xml><test/><!-- test -->    test    <![CDATA[       Can contain literal <XML> tags      ]]><!-- test --> test 2 <?xml-stylesheet type = 'text/xsl'  href  =   show_book.xsl?></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals24()
            Dim code = "Dim         xml         =           <xml>     <test>   <%=    <xml>     <test id=""42""><%=42   %>   <%=      ""hello""%>   </test>  </xml>   %>             </test>  </xml>"
            Dim expected = "        Dim xml = <xml><test><%= <xml><test id=""42""><%= 42 %><%= ""hello"" %></test></xml> %></test></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals25()
            Dim code = "Dim         xml         =           <xml attr=""1"">    </xml>          "
            Dim expected = "        Dim xml = <xml attr=""1""></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals26()
            Dim code = My.Resources.XmlLiterals.Test1_Input
            Dim expected = My.Resources.XmlLiterals.Test1_Output

            AssertFormat(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals27()
            Dim code = My.Resources.XmlLiterals.Test2_Input
            Dim expected = My.Resources.XmlLiterals.Test2_Output

            AssertFormat(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlLiterals28()
            Dim code = My.Resources.XmlLiterals.Test3_Input
            Dim expected = My.Resources.XmlLiterals.Test3_Output

            AssertFormat(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub AttributeOnClass1()
            Dim code = <Code><![CDATA[Namespace SomeNamespace
<SomeAttribute()>
    Class Foo
    End Class
End Namespace]]></Code>

            Dim expected = <Code><![CDATA[Namespace SomeNamespace
    <SomeAttribute()>
    Class Foo
    End Class
End Namespace]]></Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(1087167)>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub MultipleAttributesOnClass()
            Dim code = <Code><![CDATA[Namespace SomeNamespace
<SomeAttribute()>
            <SomeAttribute2()>
        Class Foo
    End Class
End Namespace]]></Code>

            Dim expected = <Code><![CDATA[Namespace SomeNamespace
    <SomeAttribute()>
    <SomeAttribute2()>
    Class Foo
    End Class
End Namespace]]></Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(1087167)>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub MultipleAttributesOnParameter_1()
            Dim code = <Code><![CDATA[Class Program
    Sub P(
                <Foo>
                        <Foo>
                    som As Integer)
    End Sub
End Class


Public Class Foo
    Inherits Attribute
End Class]]></Code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(1087167)>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub MultipleAttributesOnParameter_2()
            Dim code = <Code><![CDATA[Class Program
    Sub P(
                        <Foo>
                    som As Integer)
    End Sub
End Class


Public Class Foo
    Inherits Attribute
End Class]]></Code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(1087167)>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub MultipleAttributesOnParameter_3()
            Dim code = <Code><![CDATA[Class Program
    Sub P(     <Foo>
                        <Foo>
                    som As Integer)
    End Sub
End Class


Public Class Foo
    Inherits Attribute
End Class]]></Code>

            Dim expected = <Code><![CDATA[Class Program
    Sub P(<Foo>
                        <Foo>
                    som As Integer)
    End Sub
End Class


Public Class Foo
    Inherits Attribute
End Class]]></Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub InheritsImplementsOnClass()
            Dim code = <Code><![CDATA[Class SomeClass
Inherits BaseClass
        Implements IFoo

End Class]]></Code>

            Dim expected = <Code><![CDATA[Class SomeClass
    Inherits BaseClass
    Implements IFoo

End Class]]></Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub InheritsImplementsWithGenericsOnClass()
            Dim code = <Code><![CDATA[Class SomeClass(Of T)
Inherits BaseClass      (Of         T)
        Implements  IFoo     (      Of      String,
                              T)

End Class]]></Code>

            Dim expected = <Code><![CDATA[Class SomeClass(Of T)
    Inherits BaseClass(Of T)
    Implements IFoo(Of String,
                          T)

End Class]]></Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub InheritsOnInterface()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WhitespaceMethodParens()
            Dim code = <Code>Class SomeClass
    Sub Foo  (  x  As  Integer  )  
        Foo  (  42  )  
    End Sub
End Class</Code>

            Dim expected = <Code>Class SomeClass
    Sub Foo(x As Integer)
        Foo(42)
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WhitespaceKeywordParens()
            Dim code = <Code>Class SomeClass
    Sub Foo
        If(x And(y Or(z)) Then Stop
    End Sub
End Class</Code>

            Dim expected = <Code>Class SomeClass
    Sub Foo
        If (x And (y Or (z)) Then Stop
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WhitespaceLiteralAndTypeCharacters()
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
        Dim r = Foo&()
        Dim s = FooString$()
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
        Dim r = Foo&()
        Dim s = FooString$()
    End Sub
End Class]]></Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WhitespaceNullable()
            Dim code = <Code>Class Foo
    Property someprop As Integer        ?

    Function Method(arg1        ? As Integer) As Integer            ?
        Dim someVariable        ? As Integer
    End Function
End Class</Code>

            Dim expected = <Code>Class Foo
    Property someprop As Integer?

    Function Method(arg1? As Integer) As Integer?
        Dim someVariable? As Integer
    End Function
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WhitespaceArrayBraces()
            Dim code = <Code>Class Foo
    Sub Method()
        Dim arr()   ={   1,      2,          3       }       
    End Sub
End Class</Code>

            Dim expected = <Code>Class Foo
    Sub Method()
        Dim arr() = {1, 2, 3}
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WhitespaceStatementSeparator()
            Dim code = <Code>Class Foo
    Sub Method()
        Dim x=2:Dim y=3:Dim z=4  
    End Sub
End Class</Code>

            Dim expected = <Code>Class Foo
    Sub Method()
        Dim x = 2 : Dim y = 3 : Dim z = 4
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WhitespaceRemovedBeforeComment()
            Dim code = <Code>Class Foo
    Sub Method()
        Dim a = 4                           ' This is a comment that doesn't move
                                            ' This is a comment that will have some preceding whitespace removed
        Dim y = 4
    End Sub
End Class</Code>

            Dim expected = <Code>Class Foo
    Sub Method()
        Dim a = 4                           ' This is a comment that doesn't move
        ' This is a comment that will have some preceding whitespace removed
        Dim y = 4
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ReFormatWithTabsEnabled1()
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        Foo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Foo()" + vbCrLf +
                vbTab + vbTab + "Foo()" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), True},
                {New OptionKey(FormattingOptions.TabSize, LanguageNames.VisualBasic), 4},
                {New OptionKey(FormattingOptions.IndentationSize, LanguageNames.VisualBasic), 4}
            }

            AssertFormat(code, expected, changedOptionSet:=optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ReFormatWithTabsEnabled2()
            ' tabs after the first token on a line should be converted to spaces

            Dim code =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Foo()" + vbCrLf +
                vbTab + vbTab + "Foo()" + vbTab + vbTab + "'comment" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Foo()" + vbCrLf +
                vbTab + vbTab + "Foo()       'comment" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), True},
                {New OptionKey(FormattingOptions.TabSize, LanguageNames.VisualBasic), 4},
                {New OptionKey(FormattingOptions.IndentationSize, LanguageNames.VisualBasic), 4}
            }

            AssertFormat(code, expected, changedOptionSet:=optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ReFormatWithTabsEnabled3()
            ' This is a regression test for the assert: it may still not pass after the assert is fixed.

            Dim code =
                "Class SomeClass" + vbCrLf +
                "        Sub Foo()           ' Comment" + vbCrLf +
                "  Foo()    " + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Foo()           ' Comment" + vbCrLf +
                vbTab + vbTab + "Foo()" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), True},
                {New OptionKey(FormattingOptions.TabSize, LanguageNames.VisualBasic), 4},
                {New OptionKey(FormattingOptions.IndentationSize, LanguageNames.VisualBasic), 4}
            }

            AssertFormat(code, expected, changedOptionSet:=optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ReFormatWithTabsEnabled4()
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        Dim abc = Sub()" + vbCrLf +
                "                      Console.WriteLine(42)" + vbCrLf +
                "                  End Sub" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Foo()" + vbCrLf +
                vbTab + vbTab + "Dim abc = Sub()" + vbCrLf +
                vbTab + vbTab + vbTab + vbTab + vbTab + "  Console.WriteLine(42)" + vbCrLf +
                vbTab + vbTab + vbTab + vbTab + "  End Sub" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), True},
                {New OptionKey(FormattingOptions.TabSize, LanguageNames.VisualBasic), 4},
                {New OptionKey(FormattingOptions.IndentationSize, LanguageNames.VisualBasic), 4}
            }

            AssertFormat(code, expected, changedOptionSet:=optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ReFormatWithTabsEnabled5()
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        Dim abc = 2 + " + vbCrLf +
                "                    3 + " + vbCrLf +
                "                   4 " + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Foo()" + vbCrLf +
                vbTab + vbTab + "Dim abc = 2 +" + vbCrLf +
                vbTab + vbTab + vbTab + vbTab + vbTab + "3 +" + vbCrLf +
                vbTab + vbTab + vbTab + vbTab + "   4" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), True},
                {New OptionKey(FormattingOptions.TabSize, LanguageNames.VisualBasic), 4},
                {New OptionKey(FormattingOptions.IndentationSize, LanguageNames.VisualBasic), 4}
            }

            AssertFormat(code, expected, changedOptionSet:=optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ReFormatWithTabsDisabled()
            Dim code =
                "Class SomeClass" + vbCrLf +
                vbTab + "Sub Foo()" + vbCrLf +
                vbTab + vbTab + "Foo()" + vbCrLf +
                vbTab + "End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        Foo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), False},
                {New OptionKey(FormattingOptions.TabSize, LanguageNames.VisualBasic), 4},
                {New OptionKey(FormattingOptions.IndentationSize, LanguageNames.VisualBasic), 4}
            }

            AssertFormat(code, expected, changedOptionSet:=optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ReFormatWithDifferentIndent1()
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        Foo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "  Sub Foo()" + vbCrLf +
                "    Foo()" + vbCrLf +
                "  End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), False},
                {New OptionKey(FormattingOptions.TabSize, LanguageNames.VisualBasic), 4},
                {New OptionKey(FormattingOptions.IndentationSize, LanguageNames.VisualBasic), 2}
            }

            AssertFormat(code, expected, changedOptionSet:=optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ReFormatWithDifferentIndent2()
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        Foo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "      Sub Foo()" + vbCrLf +
                "            Foo()" + vbCrLf +
                "      End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), False},
                {New OptionKey(FormattingOptions.TabSize, LanguageNames.VisualBasic), 4},
                {New OptionKey(FormattingOptions.IndentationSize, LanguageNames.VisualBasic), 6}
            }

            AssertFormat(code, expected, changedOptionSet:=optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ReFormatWithTabsEnabledSmallIndentAndLargeTab()
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        Foo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "  Sub Foo()" + vbCrLf +
                vbTab + " Foo()" + vbCrLf +
                "  End Sub" + vbCrLf +
                "End Class"

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), True},
                {New OptionKey(FormattingOptions.TabSize, LanguageNames.VisualBasic), 3},
                {New OptionKey(FormattingOptions.IndentationSize, LanguageNames.VisualBasic), 2}
            }

            AssertFormat(code, expected, changedOptionSet:=optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub RegressionCommentFollowsSubsequentIndent4173()
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        'comment" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        'comment" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            AssertFormat(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub FormatUsingOverloads()
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "Foo()    " + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        Foo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            AssertFormatUsingAllEntryPoints(code, expected)
        End Sub

        <WorkItem(538533, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4173_1()
            Dim code = "Dim a = <xml>   <%=<xml></xml>%>    </xml>"
            Dim expected = "        Dim a = <xml><%= <xml></xml> %></xml>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <WorkItem(538533, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4173_2()
            Dim code = <Code>Class Foo
        Sub Foo()           ' Comment
  Foo()    
    End Sub
End Class</Code>

            Dim expected = <Code>Class Foo
	Sub Foo()           ' Comment
		Foo()
	End Sub
End Class</Code>

            Dim optionSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(FormattingOptions.UseTabs, LanguageNames.VisualBasic), True}
            }

            AssertFormatLf2CrLf(code.Value, expected.Value, optionSet)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub FormatUsingAutoGeneratedCodeOperationProvider()
            Dim code =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "Foo()    " + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            Dim expected =
                "Class SomeClass" + vbCrLf +
                "    Sub Foo()" + vbCrLf +
                "        Foo()" + vbCrLf +
                "    End Sub" + vbCrLf +
                "End Class"

            AssertFormat(code, expected)
        End Sub

        <WorkItem(538533, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4173_3()
            Dim code = My.Resources.XmlLiterals.Test4_Input
            Dim expected = My.Resources.XmlLiterals.Test4_Output

            AssertFormat(code, expected)
        End Sub

        <WorkItem(538533, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4173_4()
            Dim code = <code>Class C
            Sub Main(args As String())
Dim r = 2
                    'foo
End Sub
            End Class</code>
            Dim expected = <code>Class C
    Sub Main(args As String())
        Dim r = 2
        'foo
    End Sub
End Class</code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538533, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4173_5()
            Dim code = <code>Module Module1
    Public Sub foo
        ()
    End Sub
End Module</code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(538533, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4173_6()
            Dim code = <code>Module module1
#If True Then
#End If: foo()
End Module</code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(538533, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4173_7()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538533, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4173_8()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538533, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4173_9()
            Dim code = <code>Module Module1
    Sub Main()
            #If True Then
    Dim foo as Integer
                        #End If
        End Sub
End Module</code>

            Dim expected = <code>Module Module1
    Sub Main()
#If True Then
        Dim foo as Integer
#End If
    End Sub
End Module</code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538772, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4482()
            Dim code = <code>_' Public Function GroupBy(Of K, R)( _
_' ByVal key As KeyFunc(Of K), _
_' ByVal selector As SelectorFunc(Of K, QueryableCollection(Of T), R)) _
' As QueryableCollection(Of R)</code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(538754, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4459()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538675, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4352()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538703, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4394()
            AssertFormat(My.Resources.XmlLiterals.Test5_Input, My.Resources.XmlLiterals.Test5_Output)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub GetTypeTest()
            Dim code = "Dim a = GetType     (       Object          )"
            Dim expected = "        Dim a = GetType(Object)"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub NewObjectTest()
            Dim code = "Dim a = New Object ( )"
            Dim expected = "        Dim a = New Object()"

            AssertFormat(CreateMethod(code), CreateMethod(expected), debugMode:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub CTypeTest()
            Dim code = "Dim a = CType       (       args        ,   String  ( )   )     "
            Dim expected = "        Dim a = CType(args, String())"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TernaryConditionTest()
            Dim code = "Dim a = If (    True    , 1, 2)"
            Dim expected = "        Dim a = If(True, 1, 2)"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TryCastTest()
            Dim code = "Dim a = TryCast (       args        , String())"
            Dim expected = "        Dim a = TryCast(args, String())"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <WorkItem(538703, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4394_1()
            Dim code = My.Resources.XmlLiterals.Test6_Input
            Dim expected = My.Resources.XmlLiterals.Test6_Output

            AssertFormat(code, expected)
        End Sub

        <WorkItem(538889, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4639()
            Dim code = "Imports <xmlns=""http://DefaultNamespace""       >       "
            Dim expected = "Imports <xmlns=""http://DefaultNamespace"">"

            AssertFormat(code, expected)
        End Sub

        <WorkItem(538891, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4641()
            Dim code = <Code>Module module1
	 Structure C
 
	 End Structure
 
	 Sub foo()
		  Dim cc As C   ?   = New   C   ?   (   )   
	 End Sub
End Module</Code>

            Dim expected = <Code>Module module1
    Structure C

    End Structure

    Sub foo()
        Dim cc As C? = New C?()
    End Sub
End Module</Code>
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538892, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4642()
            Dim code = <Code>_      
    </Code>

            Dim expected = <Code> _
</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538894, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4644()
            Dim code = <Code>Option Explicit Off
Module Module1
    Sub Main()
        Dim mmm = Sub(ByRef x As String, _
                                y As Integer)
                      Console.WriteLine(x &amp; y)
                  End Sub, kkk = Sub(y, _
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
                  End Sub, kkk = Sub(y, _
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538897, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4647()
            Dim code = <Code>Option Explicit Off
Module Module1
    Sub Main()
        Dim mmm = Sub(ByRef x As String, _
                                y As Integer)
                      Console.WriteLine(x &amp; y)
                  End Sub, kkk = Sub(y, _
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
                  End Sub, kkk = Sub(y, _
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(538962, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub WorkItem4737()
            Dim code = "Dim x = <?xml version =""1.0""?><code></code>"
            Dim expected = "        Dim x = <?xml version=""1.0""?><code></code>"

            AssertFormat(CreateMethod(code), CreateMethod(expected))
        End Sub

        <WorkItem(539031, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix4826()
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

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Parentheses()
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

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(539170, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5022()
            Dim code = <Code>Class A
: _
Dim x
End Class</Code>

            Dim expected = <Code>Class A
    : _
    Dim x
End Class</Code>
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539324, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5232()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539353, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5270()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539358, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5277()
            Dim code = <Code>
#If True Then
               #End If
</Code>

            Dim expected = <Code>
#If True Then
#End If
</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539455, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5432()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539351, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5268()
            Dim code = <Code>
#If True _
</Code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(539351, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5268_1()
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
#If True _

End Module</Code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(539473, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5456()
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim foo = New With {.foo = "foo",.bar = "bar"}
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim foo = New With {.foo = "foo", .bar = "bar"}
    End Sub
End Module</Code>
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539474, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5457()
            Dim code = <Code>Module module1
    Sub main()
        Dim var1 As New Class1
        [|var1.foofoo = 42

    End Sub
    Sub something()
        something()
    End Sub
End Module

Public Class Class1
    Public foofoo As String|]
End Class</Code>

            Dim expected = <Code>Module module1
    Sub main()
        Dim var1 As New Class1
        var1.foofoo = 42

    End Sub
    Sub something()
        something()
    End Sub
End Module

Public Class Class1
    Public foofoo As String
End Class</Code>

            AssertFormatSpan(code.Value.Replace(vbLf, vbCrLf), expected.Value.Replace(vbLf, vbCrLf))
        End Sub

        <WorkItem(539503, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5492()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539508, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5497()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539581, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5594()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539582, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5595()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539616, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5637()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlTest()
            AssertFormat(My.Resources.XmlLiterals.XmlTest1_Input, My.Resources.XmlLiterals.XmlTest1_Output)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlDocument()
            AssertFormat(My.Resources.XmlLiterals.XmlTest2_Input, My.Resources.XmlLiterals.XmlTest2_Output)
        End Sub

        <Fact>
        <WorkItem(539458, "DevDiv")>
        <WorkItem(539459, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlProcessingInstruction()
            AssertFormat(My.Resources.XmlLiterals.XmlTest3_Input, My.Resources.XmlLiterals.XmlTest3_Output)
        End Sub

        <WorkItem(539463, "DevDiv")>
        <WorkItem(530597, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlTest5442()
            Using workspace = New AdhocWorkspace()

                Dim project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.VisualBasic)
                Dim document = project.AddDocument("Document", SourceText.From(My.Resources.XmlLiterals.XmlTest4_Input_Output))
                Dim root = document.GetSyntaxRootAsync().Result

                ' format first time
                Dim result = Formatter.GetFormattedTextChanges(root, workspace)
                AssertResult(My.Resources.XmlLiterals.XmlTest4_Input_Output, document.GetTextAsync().Result, result)

                Dim document2 = document.WithText(document.GetTextAsync().Result.WithChanges(result))
                Dim root2 = document2.GetSyntaxRootAsync().Result

                ' format second time
                Dim result2 = Formatter.GetFormattedTextChanges(root, workspace)
                AssertResult(My.Resources.XmlLiterals.XmlTest4_Input_Output, document2.GetTextAsync().Result, result2)
            End Using
        End Sub

        <WorkItem(539687, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5731()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539545, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5547()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539453, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5430()
            Dim code = My.Resources.XmlLiterals.IndentationTest1

            AssertFormat(code, code)
        End Sub

        <WorkItem(539889, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix5989()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539409, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix6367()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(539409, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix6367_1()
            Dim code = <Code>Module Program
    Sub Main(args As String())
        Dim a As Integer = 1    'Test
    End Sub
End Module
</Code>
            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(540678, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BugFix7023_1()
            Dim code = <Code>Module Program
    Public Operator +(x As Integer, y As Integer)
        Console.WriteLine("FOO")
    End Operator
End Module
</Code>
            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlTextWithEmbededExpression1()
            AssertFormat(My.Resources.XmlLiterals.XmlTest5_Input, My.Resources.XmlLiterals.XmlTest5_Output)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlTextWithEmbededExpression2()
            AssertFormat(My.Resources.XmlLiterals.XmlTest6_Input, My.Resources.XmlLiterals.XmlTest6_Output)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlText()
            AssertFormat(My.Resources.XmlLiterals.XmlTest7_Input, My.Resources.XmlLiterals.XmlTest7_Output)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlTextWithComment()
            AssertFormat(My.Resources.XmlLiterals.XmlTest8_Input, My.Resources.XmlLiterals.XmlTest8_Output)
        End Sub

        <WorkItem(541628, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub MultipleControlVariables()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <WorkItem(541561, "DevDiv")>
        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ColonTrivia()
            Dim code = <Code>Module Program
                        Sub Foo3()
                : End Sub

    Sub Main()
    End Sub
End Module</Code>

            Dim expected = <Code>Module Program
    Sub Foo3()
    : End Sub

    Sub Main()
    End Sub
End Module</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub MemberAccessInObjectMemberInitializer()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BinaryConditionalExpression()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(539574, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub Preprocessors()
            Dim code = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
 
#Const [PUBLIC]    =    3
#Const     foo = 23
#Const foo2=23
#Const foo3 = 23
#Const  foo4  =  23
#Const     foo5    =           23
 
 
    End Sub
End Module</Code>

            Dim expected = <Code>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

#Const [PUBLIC] = 3
#Const foo = 23
#Const foo2 = 23
#Const foo3 = 23
#Const foo4 = 23
#Const foo5 = 23


    End Sub
End Module</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(10027, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub RandomCode1()
            Dim code = <Code>'Imports alias 'foo' conflicts with 'foo' declared in the root namespace'</Code>
            Dim expected = <Code>'Imports alias 'foo' conflicts with 'foo' declared in the root namespace'</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(10027, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub RandomCode2()
            Dim code = <Code>'Imports alias 'foo' conflicts with 'foo' declared in the root 
namespace'</Code>
            Dim expected = <Code>'Imports alias 'foo' conflicts with 'foo' declared in the root 
namespace'</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(542698, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ColonTrivia1()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(542698, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ColonTrivia2()
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
            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <Fact>
        <WorkItem(543197, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub KeyInAnonymousType()
            Dim code = <Code>Class C
    Sub S()
        Dim product = New With {Key.Name = "foo"}
    End Sub
End Class
</Code>

            Dim expected = <Code>Class C
    Sub S()
        Dim product = New With {Key .Name = "foo"}
    End Sub
End Class
</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(544008, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TestGetXmlNamespace()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(539409, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub StructuredTrivia()
            Dim code = <Code>#const foo=2.0d</Code>
            Dim expected = <Code>#const foo = 2.0d</Code>
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub FormatComment1()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub FormatComment2()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(543248, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub FormatBadCode()
            Dim code = <Code>Imports System
Class Program
    Shared Sub Main(args As String())
        SyncLock From y As Char  i, j As Char In String.Empty

        End SyncLock
    End Sub
End Class</Code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <Fact>
        <WorkItem(544496, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub AttributeInParameterList()
            Dim code = <Code>Module Program
    Sub Main(           &lt;Description&gt; args As String())
    End Sub
End Module</Code>

            Dim expected = <Code>Module Program
    Sub Main(&lt;Description&gt; args As String())
    End Sub
End Module</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(544980, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlElement_Expression()
            Dim code = <Code>Module Program
    Dim x = &lt;x       &lt;%=      "" %&gt;        /&gt;
End Module</Code>

            Dim expected = <Code>Module Program
    Dim x = &lt;x &lt;%= "" %&gt;/&gt;
End Module</Code>

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(542976, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlElementStartTag()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(545088, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ForNext_MultipleVariables()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(545088, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ForNext_MultipleVariables2()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(544459, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub DictionaryAccessOperator()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact>
        <WorkItem(542976, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XmlElementStartTag1()
            AssertFormat(My.Resources.XmlLiterals.XmlElementStartTag1_Input, My.Resources.XmlLiterals.XmlElementStartTag1_Output)
        End Sub

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

            Dim foo As New SyntaxAnnotation()
            Dim implementsStatement = DirectCast(root.Members(0), ClassBlockSyntax).Implements.First()
            root = root.ReplaceNode(implementsStatement, implementsStatement.NormalizeWhitespace(elasticTrivia:=True).WithAdditionalAnnotations(foo))

            Dim field = DirectCast(root.Members(0), ClassBlockSyntax).Members(0)
            root = root.ReplaceNode(field, field.NormalizeWhitespace(elasticTrivia:=True).WithAdditionalAnnotations(foo))
            Dim prop = DirectCast(root.Members(0), ClassBlockSyntax).Members(1)
            root = root.ReplaceNode(prop, prop.NormalizeWhitespace(elasticTrivia:=True).WithAdditionalAnnotations(foo))

            Dim method = DirectCast(root.Members(0), ClassBlockSyntax).Members(2)
            root = root.ReplaceNode(method, method.NormalizeWhitespace(elasticTrivia:=True).WithAdditionalAnnotations(foo))

            Using workspace = New AdhocWorkspace()
                Dim result = Formatter.Format(root, foo, workspace).ToString()
                Assert.Equal(expected, result)
            End Using
        End Sub

        <Fact>
        <WorkItem(545630, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub SpacesInXmlStrings()
            Dim text = "Imports <xmlns:x='&#70;'>"

            AssertFormat(text, text)
        End Sub

        <Fact>
        <WorkItem(545680, "DevDiv")>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub SpaceBetweenPercentGreaterThanAndXmlName()
            Dim text = <code>Module Program
    Sub Main(args As String())
    End Sub
    Public Function GetXml() As XElement
        Return &lt;field name=&lt;%= 1 %&gt; type=&lt;%= 2 %&gt;&gt;&lt;/field&gt;
    End Function
End Module</code>

            AssertFormatLf2CrLf(text.Value, text.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub SpacingAroundXmlEntityLiterals()
            Dim code =
<Code><![CDATA[Class C
    Sub Bar()
        Dim foo = <Code>&lt;&gt;</Code>
    End Sub
End Class
]]></Code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <WorkItem(547005, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub BadDirectivesAreValidRanges()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(17313, "DevDiv_Projects/Roslyn")>
        Public Sub TestElseIfFormatting_Directive()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(529899, "DevDiv_Projects/Roslyn")>
        Public Sub IndentContinuedLineOfSingleLineLambdaToFunctionKeyword()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(604032, "DevDiv_Projects/Roslyn")>
        Public Sub TestSpaceBetweenEqualsAndDotOfXml()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(604092, "DevDiv_Projects/Roslyn")>
        Public Sub AnchorIndentToTheFirstTokenOfXmlBlock()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(530366, "DevDiv_Projects/Roslyn")>
        Public Sub ForcedSpaceBetweenXmlNameTokenAndPercentGreaterThanToken()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(531444, "DevDiv")>
        Public Sub TestElseIfFormattingForNestedSingleLineIf()
            Dim code =
<Code><![CDATA[
        If True Then Console.WriteLine(1) Else If True Then Return
]]></Code>

            Dim actual = CreateMethod(code.Value)

            ' Verify "Else If" doesn't get formatted to "ElseIf"
            AssertFormatLf2CrLf(actual, actual)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TestDontCrashOnMissingTokenWithComment()
            Dim code =
<Code><![CDATA[
Namespace NS
    Class CL
        Sub Method()
            Dim foo = Sub(x) 'Comment
]]></Code>

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TestBang()
            Dim code =
<Code><![CDATA[
Imports System.Collections
 
Module Program
    Sub Main()
        Dim x As New Hashtable
        Dim y = x               !                    _
        Foo
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
        Foo
    End Sub
End Module
]]></Code>
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(679864, "DevDiv")>
        Public Sub InsertSpaceBetweenXMLMemberAttributeAccessAndEqualsToken()
            Dim expected =
<Code><![CDATA[
Imports System
Imports System.Collections

Module Program
    Sub Main(args As String())
        Dim element = <element></element>
        Dim foo = element.Single(Function(e) e.@Id = 1)
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
        Dim foo = element.Single(Function(e) e.@Id     =    1)
    End Sub
End Module
]]></Code>
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(923172, "DevDiv")>
        Public Sub TestMemberAccessAfterOpenParen()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(923180, "DevDiv")>
        Public Sub TestXmlMemberAccessDot()
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
            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(530601, "DevDiv")>
        Public Sub TestElasticFormattingPropertySetter()
            Dim parameterList = SyntaxFactory.ParseParameterList(String.Format("(value As {0})", "Integer"))
            Dim setter = SyntaxFactory.AccessorBlock(SyntaxKind.SetAccessorBlock,
                                                   SyntaxFactory.AccessorStatement(SyntaxKind.SetAccessorStatement, SyntaxFactory.Token(SyntaxKind.SetKeyword)).
                                                                 WithParameterList(parameterList),
                                                   SyntaxFactory.EndBlockStatement(SyntaxKind.EndSetStatement, SyntaxFactory.Token(SyntaxKind.SetKeyword)))
            Dim setPropertyStatement = SyntaxFactory.ParseExecutableStatement(String.Format("SetProperty({0}, value, ""{1}"")", "field", "Property")).WithLeadingTrivia(SyntaxFactory.ElasticMarker)
            setter = setter.WithStatements(SyntaxFactory.SingletonList(setPropertyStatement))

            Dim solution = New AdhocWorkspace().CurrentSolution
            Dim project = solution.AddProject("proj", "proj", LanguageNames.VisualBasic)
            Dim document = project.AddDocument("foo.vb", <text>Class C
    WriteOnly Property Prop As Integer
    End Property
End Class</text>.Value)

            Dim propertyBlock = document.GetSyntaxRootAsync().Result.DescendantNodes().OfType(Of PropertyBlockSyntax).Single()
            document = Formatter.FormatAsync(document.WithSyntaxRoot(document.GetSyntaxRootAsync().Result.ReplaceNode(propertyBlock, propertyBlock.WithAccessors(SyntaxFactory.SingletonList(setter))))).Result

            Dim actual = document.GetTextAsync().Result.ToString()
            Assert.Equal(actual, actual)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TestWarningDirectives()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TestIncompleteWarningDirectives()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <WorkItem(796562)>
        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub TriviaAtEndOfCaseBelongsToNextCase()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <WorkItem(938188)>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub XelementAttributeSpacing()
            Dim text = <Code>
Class X
    Function F(x As Integer) As Integer
        Dim x As XElement
        x.@Foo= "Hello"
    End Function
End Class
</Code>

            Dim expected = <Code>
Class X
    Function F(x As Integer) As Integer
        Dim x As XElement
        x.@Foo = "Hello"
    End Function
End Class
</Code>

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ConditionalAccessFormatting()
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

            AssertFormat(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub ChainedConditionalAccessFormatting()
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

            AssertFormat(code, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub NameOfFormatting()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub InterpolatedString1()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub InterpolatedString2()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub InterpolatedString3()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub InterpolatedString4()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub InterpolatedString5()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <WorkItem(3293, "https://github.com/dotnet/roslyn/issues/3293")>
        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub CaseCommentsRemainsUndisturbed()
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

            AssertFormatLf2CrLf(text.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub NewLineOption_LineFeedOnly()
            Dim tree = SyntaxFactory.ParseCompilationUnit("Class C" & vbCrLf & "End Class")

            ' replace all EOL trivia with elastic markers to force the formatter to add EOL back
            tree = tree.ReplaceTrivia(tree.DescendantTrivia().Where(Function(tr) tr.IsKind(SyntaxKind.EndOfLineTrivia)), Function(o, r) SyntaxFactory.ElasticMarker)

            Dim formatted = Formatter.Format(tree, DefaultWorkspace, DefaultWorkspace.Options.WithChangedOption(FormattingOptions.NewLine, LanguageNames.VisualBasic, vbLf))
            Dim actual = formatted.ToFullString()

            Dim expected = "Class C" & vbLf & "End Class"

            Assert.Equal(expected, actual)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(2822, "https://github.com/dotnet/roslyn/issues/2822")>
        Public Sub FormatLabelFollowedByDotExpression()
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

            AssertFormatLf2CrLf(code.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(2822, "https://github.com/dotnet/roslyn/issues/2822")>
        Public Sub FormatOmittedArgument()
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

            AssertFormatLf2CrLf(code.Value, code.Value)
        End Sub

    End Class
End Namespace