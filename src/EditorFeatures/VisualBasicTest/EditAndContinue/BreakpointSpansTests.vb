' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    <Trait(Traits.Feature, Traits.Features.DebuggingBreakpoints)>
    Public Class BreakpointSpansTests

#Region "Helpers"

        Private Sub TestSpan(markup As String)
            Test(markup, isMissing:=False, isLine:=False)
        End Sub

        Private Sub TestSpan(markup As XElement)
            Test(markup.NormalizedValue, isMissing:=False, isLine:=False)
        End Sub

        Private Sub TestMissing(markup As XElement)
            Test(markup.NormalizedValue, isMissing:=True, isLine:=False)
        End Sub

        Private Sub TestLine(markup As XElement)
            Test(markup.NormalizedValue, isMissing:=False, isLine:=True)
        End Sub

        Private Sub TestAll(markup As XElement)
            TestAll(markup.NormalizedValue)
        End Sub

        Private Sub Test(markup As String, isMissing As Boolean, isLine As Boolean)
            Dim position As Integer? = Nothing
            Dim expectedSpan As TextSpan? = Nothing
            Dim source As String = Nothing
            MarkupTestFile.GetPositionAndSpan(markup, source, position, expectedSpan)

            Dim tree = SyntaxFactory.ParseSyntaxTree(source)

            Dim breakpointSpan As TextSpan
            Dim hasBreakpoint = BreakpointSpans.TryGetBreakpointSpan(tree,
                                                                      position.Value,
                                                                      CancellationToken.None,
                                                                      breakpointSpan)

            If isLine Then
                Assert.True(hasBreakpoint)
                Assert.True(breakpointSpan.Length = 0)
            ElseIf isMissing Then
                Assert.False(hasBreakpoint)
            Else
                Assert.True(hasBreakpoint)
                Assert.True(expectedSpan.Value = breakpointSpan,
                            String.Format(vbCrLf & "Expected: {0} ""{1}""" & vbCrLf & "Actual: {2} ""{3}""",
                                          expectedSpan.Value,
                                          source.Substring(expectedSpan.Value.Start, expectedSpan.Value.Length),
                                          breakpointSpan,
                                          source.Substring(breakpointSpan.Start, breakpointSpan.Length)))
            End If
        End Sub


        Private Sub TestAll(markup As String)
            Dim position As Integer = Nothing
            Dim expectedSpans As ImmutableArray(Of TextSpan) = Nothing
            Dim source As String = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, source, position, expectedSpans)

            Dim tree = SyntaxFactory.ParseSyntaxTree(source)
            Dim root = tree.GetRoot()

            Dim actualSpans = GetBreakpointSequence(root, position).ToArray()

            AssertEx.Equal(expectedSpans,
                           actualSpans,
                           itemSeparator:=vbCrLf,
                           itemInspector:=Function(span) "[|" & source.Substring(span.Start, span.Length) & "|]")
        End Sub

        Public Shared Iterator Function GetBreakpointSequence(root As SyntaxNode, position As Integer) As IEnumerable(Of TextSpan)
            Dim endPosition = root.Span.End
            Dim lastSpanEnd = 0
            While position < endPosition
                Dim span As TextSpan = Nothing
                If BreakpointSpans.TryGetEnclosingBreakpointSpan(root, position, minLength:=0, span) AndAlso span.End > lastSpanEnd Then
                    position = span.End
                    lastSpanEnd = span.End
                    Yield span
                Else
                    position += 1
                End If
            End While
        End Function
#End Region

        <Fact>
        Public Sub TestEmptyFile()
            TestMissing(<text>
$$
  </text>)
        End Sub

        <Fact>
        Public Sub TestTokenKind0()
            TestMissing(<text>'$$</text>)
        End Sub

#Region "Top-Level Statements"
        <Fact>
        Public Sub TopLevel()
            TestMissing(<text>Option $$Explicit</text>)
            TestMissing(<text>Imports $$Goo</text>)
            TestMissing(<text>Class C(O$$f Action) : End Class</text>)
            TestMissing(<text>Class C(Of Action) : End $$Class</text>)
            TestMissing(<text>Struc$$ture S : End Structure</text>)
            TestMissing(<text>Structure S : End Str$$ucture</text>)
            TestMissing(<text>Enum E$$ : End Enum</text>)
            TestMissing(<text>Enum E : End $$Enum</text>)
            TestMissing(<text>Interface E$$ : End Interface</text>)
            TestMissing(<text>Interface E : End $$Interface</text>)
            TestMissing(<text>Module E$$ : End Module</text>)
            TestMissing(<text>Module E : End $$Module</text>)
            TestMissing(<text>Namespace E$$ : End Namespace</text>)
            TestMissing(<text>Namespace E : End $$Namespace</text>)
        End Sub

        <Fact>
        Public Sub CustomProperties1()
            TestMissing(<text>
Class C
    P$$roperty P
        Get
        End Get
        Set(value)
        End Set
    End Property
End Class
</text>)
        End Sub

        <Fact>
        Public Sub CustomProperties2()
            TestMissing(<text>
Class C
    Property P
        Get
        End Get
        Set(value)
        End Set
    End$$ Property
End Class
</text>)
        End Sub

        <Fact>
        Public Sub CustomEvents1()
            TestMissing(<text>
Class C
    Custom $$Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
</text>)
        End Sub

        <Fact>
        Public Sub CustomEvents2()
            TestMissing(<text>
Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End $$Event
End Class
</text>)
        End Sub

#End Region

#Region "Methods, Constructors, Operators, Accessors"
        <Fact>
        Public Sub Sub_Header()
            TestSpan(<text>
Class C
  [|$$Sub Goo()|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Sub_Header_WithAttributes()
            TestSpan("
Class C
  <A>[|$$Sub Goo()|]
  End Sub
End Class
")
        End Sub

        <Fact>
        Public Sub Sub_Header_WithImplementsClause()
            TestSpan("
Class C
  [|$$Sub Goo() Implements I.Goo|]
  End Sub
End Class
")
        End Sub

        <Fact>
        Public Sub Sub_End()
            TestSpan(<text>
Class C
  Sub Goo()
  [|$$End Sub|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub SubNew()
            TestSpan(<text>
Class C
  [|Sub $$New()|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub SubNew_WithAttributes()
            TestSpan("
Class C
  <A>[|Sub $$New()|]
  End Sub
End Class
")
        End Sub

        <Fact>
        Public Sub Function1()
            TestSpan(<text>
Class C
  [|$$Function Goo()|]
  End Function
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Function2()
            TestSpan(<text>
Class C
  Function Goo()
  [|$$End Function|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Function_WithAttributes()
            TestSpan("
Class C
  <A>[|$$Function Goo()|]
  End Function
End Class
")
        End Sub

        <Fact>
        Public Sub Function_WithImplementsClause()
            TestSpan("
Class C
  [|$$Function Goo() Implements I.F|]
  End Function
End Class
")
        End Sub

        <Fact>
        Public Sub Operator1()
            TestSpan(<text>
Class C
  [|Shared $$Operator *(a As C, b As C) As Integer|]
  End Operator
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Operator2()
            TestSpan(<text>
Class C
  Shared Operator *(a As C, b As C) As Integer
  [|End $$Operator|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Operator_WithAttributes()
            TestSpan("
Class C
  <A>[|Shared $$Operator *(a As C, b As C) As Integer|]
  End Operator
End Class
")
        End Sub

        <Fact>
        Public Sub Get1()
            TestSpan(<text>
Class C
    Property P
        [|G$$et|]
        End Get
        Set(value)
        End Set
    End Property
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Get2()
            TestSpan(<text>
Class C
    Property P
        Get
        [|End Get|]  $$
        Set(value)
        End Set
    End Property
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Get_WithAttributes()
            TestSpan("
Class C
    Property P
        <A>[|G$$et|]
        End Get
        Set(value)
        End Set
    End Property
End Class
")
        End Sub

        <Fact>
        Public Sub Set1()
            TestSpan(<text>
Class C
    Property P
        Get
        End Get
        [|Set($$value)|]
        End Set
    End Property
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Set2()
            TestSpan(<text>
Class C
    Property P
        Get
        End Get
        Set(value)
        [|End Set|]  $$
    End Property
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Set_WithAttributes()
            TestSpan("
Class C
    Property P
        Get
        End Get
        <A>[|Set($$value)|]
        End Set
    End Property
End Class
")
        End Sub

        <Fact>
        Public Sub AddHandler1()
            TestSpan(<text>
Class C
    Custom Event E As Action
$$      [|AddHandler(value As Action)|]
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
</text>)
        End Sub

        <Fact>
        Public Sub AddHandler2()
            TestSpan(<text>
Class C
    Custom Event E As Action
        AddHandler(value As Action)
$$      [|End AddHandler|]
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
</text>)
        End Sub

        <Fact>
        Public Sub AddHandler_WithAttributes()
            TestSpan("
Class C
    Custom Event E As Action
$$      <A>[|AddHandler(value As Action)|]
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
")
        End Sub

        <Fact>
        Public Sub RemoveHandler1()
            TestSpan(<text>
Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        [|Remove$$Handler(value As Action)|]
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
</text>)
        End Sub

        <Fact>
        Public Sub RemoveHandler2()
            TestSpan(<text>
Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        [|End Remove$$Handler|]
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
</text>)
        End Sub

        <Fact>
        Public Sub RemoveHandler_WithAttributes()
            TestSpan("
Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        <A>[|Remove$$Handler(value As Action)|]
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
")
        End Sub

        <Fact>
        Public Sub RaiseEvent1()
            TestSpan(<text>
Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        [|RaiseEvent($$)|]
        End RaiseEvent
    End Event
End Class
</text>)
        End Sub

        <Fact>
        Public Sub RaiseEvent2()
            TestSpan(<text>
Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        [|End $$RaiseEvent|]
    End Event
End Class
</text>)
        End Sub

        <Fact>
        Public Sub RaiseEvent_WithAttributes()
            TestSpan("
Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        <A>[|RaiseEvent($$)|]
        End RaiseEvent
    End Event
End Class
")
        End Sub

#End Region

#Region "Auto-Properties"
        <Fact>
        Public Sub AutoProperty_NoInitializer1()
            TestMissing(<text>
Class C
    Property P$$
End Class
</text>)
        End Sub
        <Fact>
        Public Sub AutoProperty_NoInitializer2()
            TestMissing(<text>
Class C
    Property P As Integer$$
End Class
</text>)
        End Sub

        <Fact>
        Public Sub AutoProperty_Initializer1()
            TestSpan(<text>
Class C
    $$Property [|P As Integer = 1|] Implements I.P
End Class
</text>)
        End Sub

        <Fact>
        Public Sub AutoProperty_Initializer2()
            TestSpan(<text>
Class C
    Property [|P $$As Integer = 1|] Implements I.P
End Class
</text>)
        End Sub

        <Fact>
        Public Sub AutoProperty_Initializer3()
            TestSpan(<text>
Class C
    Property [|P As Integer = 1|] Implements $$I.P
End Class
</text>)
        End Sub

        <Fact>
        Public Sub AutoProperty_Initializer4()
            TestSpan(<text>
Class C
    Property [|P = 1|] Implements $$I.P
End Class
</text>)
        End Sub

        <Fact>
        Public Sub AutoProperty_AsNewInitializer1()
            TestSpan(<text>
Class C
    $$Property [|P As New C()|] Implements I.P
End Class
</text>)
        End Sub
#End Region

#Region "Fields"

        <Fact>
        Public Sub Field_NoInitializer1()
            TestMissing(<text>
Class C
    Dim A As Integer$$
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_NoInitializer2()
            TestMissing(<text>
Class C
    Dim a, b$$
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_NoInitializer3()
            TestMissing(<text>
Class C
    Dim a$$, b
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_NoInitializer4()
            TestSpan(<text>
Class C
    Dim a $$As Integer, [|b = 1|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_NoInitializer5()
            TestSpan(<text>
Class C
    Dim [|a As Integer = 1|], b As Integer$$, c As Integer = 1
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_NoInitializer6()
            TestSpan(<text>
Class C
    Dim a As Integer = 1, b As Integer,$$ c As Integer, [|d As Integer = 1|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_NoInitializer7()
            TestSpan(<text>
Class C
    Dim $$ a As Integer, b As Integer, c As Integer, [|d As New Integer|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_NoInitializer8()
            TestSpan(<text>
Class C
    Dim [|a As New Integer|], b As Integer, c As Integer, $$ d As Integer
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Const1()
            TestMissing(<text>
Class C
  Const A As Integer = $$0
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Const2()
            TestMissing(<text>
Class C
  Con$$st A As Integer = 0
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Const3()
            TestMissing(<text>
Class C
  Const A$$A As Integer = 0
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_Untyped1()
            TestSpan(<text>
Class C
$$  Dim [|a = 1|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_Untyped2()
            TestSpan(<text>
Class C
  Dim$$ [|a = 1|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_Untyped3()
            TestSpan(<text>
Class C
  Dim [|a$$a = 1|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_Untyped4()
            TestSpan(<text>
Class C
  Dim [|aa = 1$$|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_Untyped5()
            TestSpan(<text>
Class C
  Dim [|a = 1|]   $$
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_Untyped1()
            TestSpan(<text>
Class C
$$  Dim [|a = 1|], b = 1
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_Untyped2()
            TestSpan(<text>
Class C
  Dim$$ [|a = 1|], b = 1
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_Untyped3()
            TestSpan(<text>
Class C
  Dim [|$$a = 1|], b = 1
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_Untyped4()
            TestSpan(<text>
Class C
  Dim [|a = 1|]$$, b = 1
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_Untyped5()
            TestSpan(<text>
Class C
  Dim a = 1,$$ [|b = 1|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_Untyped6()
            TestSpan(<text>
Class C
  Dim a = 1, [|b = 1|]  $$
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_Typed1()
            TestSpan(<text>
Class C
$$  Dim [|a As Integer = 1|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_Typed2()
            TestSpan(<text>
Class C
  Dim [|a As Int$$eger = 1|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_Typed3()
            TestSpan(<text>
Class C
  Dim [|a As Integer = 1$$|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_Typed()
            ' syntax error, so it doesn't really matter what we do as long as it's not totally off
            TestSpan(<text>
Class C
  Dim [|a$$|], b As Integer = 1
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_AsNew1()
            TestSpan(<text>
Class C
$$  Dim [|a As New C()|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_AsNew2()
            TestSpan(<text>
Class C
  Dim [|a$$a As New C()|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_AsNew3()
            TestSpan(<text>
Class C
  Dim [|a As New $$C()|]   
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_AsNew4()
            TestSpan(<text>
Class C
  Dim [|a As New C()|]     $$
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_AsNew1()
            TestSpan(<text>
Class C
$$  Dim [|a|], b As New C()
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_AsNew2()
            TestSpan(<text>
Class C
  Dim $$[|a|], b As New C()
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_AsNew3()
            TestSpan(<text>
Class C
  Dim [|a|]$$, b As New C()
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_AsNew4()
            TestSpan(<text>
Class C
  Dim a,$$ [|b|] As New C()
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_AsNew5()
            TestSpan(<text>
Class C
  Dim a, [|b|] As $$New C()
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_AsNew6()
            TestSpan(<text>
Class C
  Dim a, [|b|] As New C()   $$
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_ArrayInitialized_Untyped1()
            TestSpan(<text>
Class C
  Dim [|b(1)|]   $$
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_ArrayInitialized_Untyped2()
            TestSpan(<text>
Class C
  Dim [|b($$1)|]   
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_ArrayInitialized_Untyped3()
            TestSpan(<text>
Class C
  $$Dim [|b(1)|]   
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_ArrayInitialized_Typed1()
            TestSpan(<text>
Class C
  Dim [|b(1)|] As Integer   $$
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_ArrayInitialized_Typed2()
            TestSpan(<text>
Class C
  Dim [|b($$1)|] As Integer   
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Single_ArrayInitialized_Typed3()
            TestSpan(<text>
Class C
  $$Dim [|b(1)|] As Integer   
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_ArrayInitializedAndInitializerMix1()
            TestSpan(<text>
Class C
  Dim a, b, c(1), d, [|e(1,2)|], f As Integer, $$g As Double, h As Single = 1.0, i(4)
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_ArrayInitializedAndInitializerMix2()
            TestSpan(<text>
Class C
  Dim a, b, c(1), d, [|e(1,2)|], $$f As Integer, g As Double, h As Single = 1.0, i(4)
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_ArrayInitializedAndInitializerMix3()
            TestSpan(<text>
Class C
  Dim a,  $$b, [|c(1)|], d, e(1,2), f As Integer, g As Double, h As Single = 1.0, i(4)
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_ArrayInitializedAndInitializerMix4()
            TestSpan(<text>
Class C
  Dim a,  b, c(1), d, e(1,2), f As Integer, g As Double, h As Single = 1.0,$$ [|i(4)|]
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_ArrayInitializedAndInitializerMix5()
            TestSpan(<text>
Class C
  Dim a,  b, c(1), d, e(1,2), f As Integer, g As Double, [|h As Single = 1.0|]$$, i(4)
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_MixAll1()
            TestSpan(<text>
Class C
  Dim $$a As Integer, [|b|], c, d As New B(), e As Boolean(), f(1) As Integer, g() As Boolean, h = 2, i As Integer = 3, j
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_MixAll2()
            TestSpan(<text>
Class C
  Dim a As Integer, b,$$ [|c|], d As New B(), e As Boolean(), f(1) As Integer, g() As Boolean, h = 2, i As Integer = 3, j
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_MixAll3()
            TestSpan(<text>
Class C
  Dim a As Integer, b, c, [|d|] As New B(), e As $$Boolean(), f(1) As Integer, g() As Boolean, h = 2, i As Integer = 3, j
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_MixAll4()
            TestSpan(<text>
Class C
  Dim a As Integer, b, c, d As New B(), e As Boolean(), [|f(1)|] As Integer, $$g() As Boolean, h = 2, i As Integer = 3, j
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_MixAll5()
            TestSpan(<text>
Class C
  Dim a As Integer, b, c, d As New B(), e As Boolean(), f(1) As Integer, g() As Boolean, [|h = $$2|], i As Integer = 3, j
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Field_Multiple_MixAll6()
            TestSpan(<text>
Class C
  Dim a As Integer, b, c, d As New B(), e As Boolean(), f(1) As Integer, g() As Boolean, h = 2, [|i As Integer = 3|], j         $$
End Class
</text>)
        End Sub

#End Region

#Region "Local Variable Declaration"
        <Fact>
        Public Sub Local_NoInitializer1()
            TestMissing(<text>
Class C
  Sub M
    Dim A As Integer$$
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_NoInitializer2()
            TestMissing(<text>
Class C
  Sub M
    Dim a, b$$
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_NoInitializer3()
            TestMissing(<text>
Class C
  Sub M
    Dim a$$, b
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_NoInitializer4()
            TestSpan(<text>
Class C
  Sub M
    Dim a $$As Integer, [|b = 1|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_NoInitializer5()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a As Integer = 1|], b As Integer$$, c As Integer = 1
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_NoInitializer6()
            TestSpan(<text>
Class C
  Sub M
    Dim a As Integer = 1, b As Integer,$$ c As Integer, [|d As Integer = 1|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_NoInitializer7()
            TestSpan(<text>
Class C
  Sub M
    Dim $$ a As Integer, b As Integer, c As Integer, [|d As New Integer|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_NoInitializer8()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a As New Integer|], b As Integer, c As Integer, $$ d As Integer
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Const1()
            TestMissing(<text>
Class C
  Sub M
    Const A As Integer = $$0
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Const2()
            TestMissing(<text>
Class C
  Sub M
    Con$$st A As Integer = 0
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Const3()
            TestMissing(<text>
Class C
  Sub M
    Const A$$A As Integer = 0
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_Untyped1()
            TestSpan(<text>
Class C
  Sub M
$$  Dim [|a = 1|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_Untyped2()
            TestSpan(<text>
Class C
  Sub M
    Dim$$ [|a = 1|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_Untyped3()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a$$a = 1|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_Untyped4()
            TestSpan(<text>
Class C
  Sub M
    Dim [|aa = 1$$|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_Untyped5()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a = 1|]   $$
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_Untyped1()
            TestSpan(<text>
Class C
  Sub M
$$  Dim [|a = 1|], b = 1
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_Untyped2()
            TestSpan(<text>
Class C
  Sub M
    Dim$$ [|a = 1|], b = 1
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_Untyped3()
            TestSpan(<text>
Class C
  Sub M
    Dim [|$$a = 1|], b = 1
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_Untyped4()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a = 1|]$$, b = 1
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_Untyped5()
            TestSpan(<text>
Class C
  Sub M
    Dim a = 1,$$ [|b = 1|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_Untyped6()
            TestSpan(<text>
Class C
  Sub M
    Dim a = 1, [|b = 1|]  $$
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_Typed1()
            TestSpan(<text>
Class C
  Sub M
$$  Dim [|a As Integer = 1|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_Typed2()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a As Int$$eger = 1|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_Typed3()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a As Integer = 1$$|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_Typed()
            ' syntax error, so it doesn't really matter what we do as long as it's not totally off
            TestSpan(<text>
Class C
  Sub M
    Dim [|a$$|], b As Integer = 1
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_AsNew1()
            TestSpan(<text>
Class C
  Sub M
$$  Dim [|a As New C()|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_AsNew2()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a$$a As New C()|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_AsNew3()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a As New $$C()|]   
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_AsNew4()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a As New C()|]     $$
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_AsNew1()
            TestSpan(<text>
Class C
  Sub M
$$  Dim [|a|], b As New C()
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_AsNew2()
            TestSpan(<text>
Class C
  Sub M
    Dim $$[|a|], b As New C()
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_AsNew3()
            TestSpan(<text>
Class C
  Sub M
    Dim [|a|]$$, b As New C()
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_AsNew4()
            TestSpan(<text>
Class C
  Sub M
    Dim a,$$ [|b|] As New C()
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_AsNew5()
            TestSpan(<text>
Class C
  Sub M
    Dim a, [|b|] As $$New C()
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_AsNew6()
            TestSpan(<text>
Class C
  Sub M
    Dim a, [|b|] As New C()   $$
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Static()
            TestSpan(<text>
Class C
  Sub M
    Static$$ [|a As Integer = 1|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_ArrayInitialized_Untyped1()
            TestSpan(<text>
Class C
  Sub M 
      Dim [|b(1)|]   $$
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_ArrayInitialized_Untyped2()
            TestSpan(<text>
Class C
  Sub M 
      Dim [|b($$1)|]   
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_ArrayInitialized_Untyped3()
            TestSpan(<text>
Class C
  Sub M 
      $$Dim [|b(1)|]   
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_ArrayInitialized_Typed1()
            TestSpan(<text>
Class C
  Sub M 
      Dim [|b(1)|] As Integer   $$
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_ArrayInitialized_Typed2()
            TestSpan(<text>
Class C
  Sub M 
      Dim [|b($$1)|] As Integer   
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Single_ArrayInitialized_Typed3()
            TestSpan(<text>
Class C
  Sub M 
      $$Dim [|b(1)|] As Integer   
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_ArrayInitializedAndInitializerMix1()
            TestSpan(<text>
Class C
  Sub M 
      Dim a, b, c(1), d, [|e(1,2)|], f As Integer, $$g As Double, h As Single = 1.0, i(4)
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_ArrayInitializedAndInitializerMix2()
            TestSpan(<text>
Class C
  Sub M 
      Dim a, b, c(1), d, [|e(1,2)|], $$f As Integer, g As Double, h As Single = 1.0, i(4)
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_ArrayInitializedAndInitializerMix3()
            TestSpan(<text>
Class C
  Sub M 
      Dim a,  $$b, [|c(1)|], d, e(1,2), f As Integer, g As Double, h As Single = 1.0, i(4)
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_ArrayInitializedAndInitializerMix4()
            TestSpan(<text>
Class C
  Sub M 
      Dim a,  b, c(1), d, e(1,2), f As Integer, g As Double, h As Single = 1.0,$$ [|i(4)|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_ArrayInitializedAndInitializerMix5()
            TestSpan(<text>
Class C
  Sub M 
      Dim a,  b, c(1), d, e(1,2), f As Integer, g As Double, [|h As Single = 1.0|]$$, i(4)
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_MixAll1()
            TestSpan(<text>
Class C
  Sub M 
      Dim $$a As Integer, [|b|], c, d As New B(), e As Boolean(), f(1) As Integer, g() As Boolean, h = 2, i As Integer = 3, j
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_MixAll2()
            TestSpan(<text>
Class C
  Sub M 
      Dim a As Integer, b,$$ [|c|], d As New B(), e As Boolean(), f(1) As Integer, g() As Boolean, h = 2, i As Integer = 3, j
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_MixAll3()
            TestSpan(<text>
Class C
  Sub M 
      Dim a As Integer, b, c, [|d|] As New B(), e As $$Boolean(), f(1) As Integer, g() As Boolean, h = 2, i As Integer = 3, j
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_MixAll4()
            TestSpan(<text>
Class C
  Sub M 
      Dim a As Integer, b, c, d As New B(), e As Boolean(), [|f(1)|] As Integer, $$g() As Boolean, h = 2, i As Integer = 3, j
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_MixAll5()
            TestSpan(<text>
Class C
  Sub M 
      Dim a As Integer, b, c, d As New B(), e As Boolean(), f(1) As Integer, g() As Boolean, [|h = $$2|], i As Integer = 3, j
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Local_Multiple_MixAll6()
            TestSpan(<text>
Class C
  Sub M 
      Dim a As Integer, b, c, d As New B(), e As Boolean(), f(1) As Integer, g() As Boolean, h = 2, [|i As Integer = 3|], j         $$
  End Sub
End Class
</text>)
        End Sub
#End Region

#Region "Method Body Statements"
        <WorkItem(538820, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538820")>
        <Fact>
        Public Sub TestEndOfStatement()
            TestSpan(<text>
class C
  sub Goo()
    [|Console.WriteLine()$$|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub If1()
            TestSpan(<text>
Class C
  Sub M
    [|If$$ True Then|]
    End If
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Else1()
            TestSpan(<text>
Class C
  Sub M
    If True Then
    [|Else$$|]
    End If
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub ElseIf1()
            TestSpan(<text>
Class C
  Sub M
    If True Then
    [|Else$$ If False|]
    End If
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub EndIf1()
            TestSpan(<text>
Class C
  Sub M
    If True Then
    [|End $$If|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub SingleLineIf1()
            TestSpan(<text>
Class C
  Sub M
    [|If$$ True Then|] Goo() Else Bar()
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub SingleLineIf2()
            TestSpan(<text>
Class C
  Sub M
    If True Then [|Goo()|] $$
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub SingleLineIf3()
            TestSpan(<text>
Class C
  Sub M
    If True Then Goo() [|E$$lse|] Bar()
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub SingleLineIf4()
            TestSpan(<text>
Class C
  Sub M
    If True Then Goo() Else [|Bar($$)|]  
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Using1()
            TestSpan(<text>
Class C
  Sub M
    [|Using $$Goo|]
    End Using
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub EndUsing1()
            TestSpan(<text>
Class C
  Sub M
    Using Goo
    [|End$$ Using|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub SyncLock1()
            TestSpan(<text>
Class C
  Sub M
    [|SyncLock $$Goo|]
    End SyncLock
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub EndSyncLock1()
            TestSpan(<text>
Class C
  Sub M
    SyncLock Goo
    [|End$$ SyncLock|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub With1()
            TestSpan(<text>
Class C
  Sub M
    [|With $$Goo|]
    End With
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub EndWith1()
            TestSpan(<text>
Class C
  Sub M
    With Goo
    [|End$$ With|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Do1()
            TestSpan(<text>
Class C
  Sub M
    [|Do$$|]
    Loop
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Loop1()
            TestSpan(<text>
Class C
  Sub M
    Do
 $$   [|Loop|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub While1()
            TestSpan(<text>
Class C
  Sub M
    [|While True$$|]
    End While
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub EndWhile1()
            TestSpan(<text>
Class C
  Sub M
    While True
 $$   [|End While|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub For1()
            TestSpan(<text>
Class C
  Sub M
    [|For $$a = 1 To 10 Step 2|]
    Next
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub ForEach1()
            TestSpan(<text>
Class C
  Sub M
    [|ForEach $$a in b|]
    Next
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Next1()
            TestSpan(<text>
Class C
  Sub M
    For a = 1 To 10 Step 2
 $$   [|Next|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Select1()
            TestSpan(<text>
Class C
  Sub M
     [|$$Select Case a|]
     Case = 3
     Case Else
    End Select
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Case1()
            TestSpan(<text>
Class C
  Sub M
     Select Case a
     [|$$Case 3|]
     Case Else
    End Select
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Case2()
            TestSpan(<text>
Class C
  Sub M
     Select Case a
     [|$$Case &lt; 3|]
     Case Else
    End Select
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub CaseElse1()
            TestSpan(<text>
Class C
  Sub M
     Select Case a
     Case 3
     [|$$Case Else|]
    End Select
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub EndSelect1()
            TestSpan(<text>
Class C
  Sub M
     Select Case a
     Case 3
     Case Else
    [|$$End Select|]
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Try1()
            TestSpan(<text>
Class C
  Sub M
     [|Tr$$y|]
     Finally
     End Try
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Catch1()
            TestSpan(<text>
Class C
  Sub M
     Try
     [|Catch $$e As Exception|]
     End Try
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub CatchWhen1()
            TestSpan(<text>
Class C
  Sub M
     Try
     [|Catch e As Exception When $$F(e)|]
     End Try
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub Finally1()
            TestSpan(<text>
Class C
  Sub M
     Try
     [|Fi$$nally|]
     End Try
  End Sub
End Class
</text>)
        End Sub

        <Fact>
        Public Sub EndTry1()
            TestSpan(<text>
Class C
  Sub M
     Try
     Finally
     [|End $$Try|]
  End Sub
End Class
</text>)
        End Sub

#End Region

#Region "Lambdas"
        <Fact>
        Public Sub Lambda_SingleLine_Header1()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine([|Funct$$ion(x)|] x + x)
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_SingleLine_Header2()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine([|$$Async Function()|] x + x)
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_SingleLine_Header_Nested()
            TestSpan(<text>
Class C
  Sub Goo()
    Dim x = Function(a) [|$$Function(b)|] a + b
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_SingleLine_Header3()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine([|Sub($$)|] M())
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_SingleLine_Header4()
            TestSpan(<text>
Class C
  Sub Goo()
    [|Console.WriteLine( $$ Sub() M())|]
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_SingleLine_Body1()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine(Function(x) [|x $$+ x|])
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_SingleLine_Body2()
            TestSpan(<text>
Class C
  Sub Goo()
    [|Console.WriteLine(Sub(x) M()$$)|]
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_SingleLine_Body3()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine(  Sub() [|M()|]   $$        )
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_SingleLine_Body4()
            TestSpan(<text>
Class C
  Sub Goo()
    [|Console.WriteLine(  Sub() M()  
             $$        )|] 
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_SingleLine_Body5()
            TestSpan(<text>
Class C
  Sub Goo()
    Private a As New D(Function() [|$$1|])
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_MultiLine_Header1()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine([|Funct$$ion(x)|] 
                           x + x
                        End Function)
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_MultiLine_Header2()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine([|$$Async Function()|] 
                            x + x
                        End Function)
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_MultiLine_Header3()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine([|Sub($$)|] 
                        End Sub)
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_MultiLine_Header4()
            TestSpan(<text>
Class C
  Sub Goo()
    [|Console.WriteLine( $$ Sub()
                            M() 
                        End Sub)|]
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_MultiLine_Body1()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine(Function(x)
                        [|F()|]  $$
                      End Function)
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_MultiLine_Footer1()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine(Function(x)
                        F()  
                      [|End Function|]     $$    )
  End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub Lambda_MultiLine_Footer2()
            TestSpan(<text>
Class C
  Sub Goo()
    Console.WriteLine(Sub()
                      [|$$End Sub|])
  End Sub
End Class</text>)
        End Sub
#End Region

#Region "Queries"

        <Fact>
        Public Sub TestFromClause1()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in $$customers, e in [|employees|]
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestFromClause2()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers, e in [|$$employees|]
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestFromClause3()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            from e in [|$$employees|]
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestFromInQueryContinuation1()
            TestSpan(<text>
Class C
    Sub Goo()
        Dim q = From x In customers
                From e In employees
                Group e By x Into g
                From m In [|g.$$Count()|]
                Select m.blah()
    End Sub
End Class</text>)
        End Sub

        <WorkItem(544959, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544959")>
        <Fact>
        Public Sub TestBeforeFromInQueryContinuation1()
            TestSpan(<text>
Class C
    Sub Goo()
        Dim q = From x In customers
    $$          From e In [|employees|]
                Group e By x Into g
                From m In g.Count()
                Select m.blah()
    End Sub
End Class</text>)
        End Sub

        <WorkItem(544959, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544959")>
        <Fact>
        Public Sub TestBeforeFromInQueryContinuation2()
            TestSpan(<text>
Class C
    Sub Goo()
        Dim q = 
          $$    From x In customers, e In [|employees|]
                Group e By x Into g
                From m In g.Count()
                Select m.blah()
    End Sub
End Class</text>)
        End Sub

        <WorkItem(544959, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544959")>
        <Fact>
        Public Sub TestBeforeFromInQueryContinuation3()
            TestSpan(<text>
Class C
    Sub Goo()
        Dim q = From x In customers
                From e In employees
                Group e By x Into g
    $$          From m In [|g.Count()|]
                Select m.blah()
    End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub TestJoin1()
            TestSpan(<text>
class C
  sub Goo()
    dim [|q = from x in customers
            join ord in $$orders on c.Id Equals ord.Id
            select x|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestJoin2()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            join ord in orders on [|$$c.Id|] Equals ord.Id
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestJoin3()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            join ord in orders on c.Id Equals [|$$ord.Id|]
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestLet1()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let m = $$[|x.y|]
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestLet2()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = $$[|x.y|]
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestLet3()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
 $$         let n = [|0|], m = x.y
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestLet4()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = [|0|]$$, m = x.y
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestLet5()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0,$$ m = [|x.y|]
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestLet6()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = [|F($$0)|], m = x.y
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestLet7()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let $$n = [|F(0)|], m = x.y
            select x
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestSelect1()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            select [|$$x + 1|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestSelect2()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            select m = [|$$x + 1|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestSelect3()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            select [|n = 2, m = $$x + 1|]
  end sub
end class</text>)
        End Sub

        <WorkItem(544960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544960")>
        <Fact>
        Public Sub TestSelect4()
            TestSpan(<text>
Class A
    Public Sub Test()
        Dim query = From c In categories
                    Group Join p In productList On c Equals p.Category Into Group
                    Select [|Category = c, $$Products = Group|]
    End Sub
End Class</text>)
        End Sub

        <WorkItem(544963, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544963")>
        <Fact>
        Public Sub TestSelect5()
            TestSpan(<text>
Class A
    Public Sub Test()
        Dim numbers() = {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}

        Dim numsPlusOne =
            From n In numbers
            Select $$z = [|n + 1|]
            Order By z
    End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub TestSelect6()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            select [|n = 2, $$m = x + 1|]
  end sub
end class</text>)
        End Sub

        <WorkItem(544964, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544964")>
        <Fact>
        Public Sub TestBeforeSelectClause()
            TestSpan(<text>
Class A
    Public Sub Test()
        Dim q = From x In customers
                Let n = 0, m = x.y
          $$      Select [|x + 1|]
    End Sub
End Class</text>)
        End Sub

        <Fact>
        Public Sub TestWhereClauseExpression()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            where [|$$x + 1 > 0|]
  end sub
end class</text>)
        End Sub

        <WorkItem(544965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544965")>
        <Fact>
        Public Sub TestBeforeWhereClause()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
         $$   where [|x + 1 > 0|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestTakeWhile1()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            Take While [|$$x + 1 > 0|]
  end sub
end class</text>)
        End Sub

        <WorkItem(544966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544966")>
        <Fact>
        Public Sub TestBeforeTakeWhile()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
$$            Take While [|x + 1 > 0|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestSkipWhile1()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            Skip While [|$$x + 1 > 0|]
  end sub
end class</text>)
        End Sub

        <WorkItem(544966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544966")>
        <Fact>
        Public Sub TestBeforeSkipWhile()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
   $$         Skip While [|x + 1 > 0|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestOrderBy1()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            order by [|$$x|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestOrderBy2()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            order by [|$$x|] ascending
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestOrderBy3()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            order by [|$$x|] descending
  end sub
end class</text>)
        End Sub

        <WorkItem(544967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544967")>
        <Fact>
        Public Sub TestBeforeOrderBy()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
      $$      order by [|x|] descending
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestThenBy1()
            TestSpan(<text>
class C
  sub Goo()
        Dim digits() = {"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"}

        Dim sortedDigits = From d In digits
                           Select d
                           Order By d.Length, [|$$d|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestThenBy2()
            TestSpan(<text>
class C
  sub Goo()
        Dim digits() = {"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"}

        Dim sortedDigits = From d In digits
                           Select d
                           Order By [|$$d.Length|], d
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestThenBy3()
            TestSpan(<text>
class C
  sub Goo()
        Dim digits() = {"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"}

        Dim sortedDigits = From d In digits
                           Order By [|d.Le$$ngth|], d
                           Select d
  end sub
end class</text>)
        End Sub

        <WorkItem(544968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544968")>
        <Fact>
        Public Sub TestThenBy4()
            TestSpan(<text>
class C
  sub Goo()
        Dim digits() = {"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"}

        Dim sortedDigits = From d In digits
                           Order By d.Length, [|$$d|]
                           Select d
  end sub
end class</text>)
        End Sub

        <WorkItem(544967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544967")>
        <Fact>
        Public Sub TestBeforeOrderByAndThenBy()
            TestSpan(<text>
class C
  sub Goo()
        Dim digits() = {"zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"}

        Dim sortedDigits = From d In digits
             $$              Order By [|d.Length|], d
                           Select d
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestFunctionAggregation1()
            TestSpan(<text>
class C
  sub Goo()
    dim q = from x in customers
            let n = 0, m = x.y
            order by x descending
            group y = x * 10 into sum([|$$x + 1|])
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy1a()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
       $$     Group [|y = x * 10, z = x * 100|] By evenOdd = x Mod 2 Into Sum(y + 1)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy1b()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
       $$     Group y = [|x * 10|] By evenOdd = x Mod 2 Into Sum(y + 1)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy1c()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = [|x *$$ 10|] By evenOdd = x Mod 2 Into Sum(y + 1)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy2()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group [|y =$$ x * 10, z = x * 100|] By evenOdd = x Mod 2 Into Sum(y + 1)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy3()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group [|y = x * 10, z = x * 100|] $$  By   evenOdd = x Mod 2 Into Sum(y + 1)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy4()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   $$By   [|evenOdd = x Mod 2 Into x1 = Sum(y + 1), x2 = Sum(y + 2)|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy5a()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By $$  [|evenOdd = x Mod 2 Into x1 = Sum(y + 1), x2 = Sum(y + 2)|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy5b()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   $$evenOdd = [|x Mod 2|] Into x1 = Sum(y + 1), x2 = Sum(y + 2)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy5c()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   $$
                [|evenOdd = x Mod 2 Into x1 = Sum(y + 1), x2 = Sum(y + 2)|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy5d()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   
         $$       [|evenOdd = x Mod 2 Into x1 = Sum(y + 1), x2 = Sum(y + 2)|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy5f()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   $$
[|evenOdd = x Mod 2 Into x1 = Sum(y + 1), x2 = Sum(y + 2)|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy6a()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   evenOdd $$= [|x Mod 2|] Into x1 = Sum(y + 1), x2 = Sum(y + 2)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy6b()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   evenOdd = [|x $$Mod 2|] Into x1 = Sum(y + 1), x2 = Sum(y + 2)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy7a()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   [|$$evenOdd = x Mod 2, y = Mod 3|] Into x1 = Sum(y + 1), x2 = Sum(y + 2)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy7b()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   [|evenOdd = x Mod 2, y$$ = Mod 3|] Into x1 = Sum(y + 1), x2 = Sum(y + 2)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy8()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   
               [|evenOdd = x Mod 2, y = Mod 3 $$Into x1 = Sum(y + 1), x2 = Sum(y + 2)|] 
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy9()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   
               evenOdd = x Mod 2, y = Mod 3 Into x1 = Sum([|$$y + 1|]), x2 = Sum(y + 2)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy10()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   
               evenOdd = x Mod 2, y = Mod 3 Into x1 = Sum(y + 1), x2 = Sum([|$$y + 2|])
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy11()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   
               evenOdd = x Mod 2, y = Mod 3 Into x1 = S$$um([|y + 1|]), x2 = Sum(y + 2)
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy12()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   
               evenOdd = x Mod 2, y = Mod 3 Into x1 = Sum(y + 1), x2 = S$$um([|y + 2|])
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy13()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   
               [|evenOdd = x Mod 2, y = Mod 3 Into x1 = Sum(y + 1),$$ x2 = Sum(y + 2)|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub GroupBy14()
            TestSpan(<text>
class C
  sub Goo()
    Dim q = From x In Nums()
            Let n = 0, m = x.ToString()
            Order By x Descending
            Group y = x * 10, z = x * 100   By   
               [|evenOdd = x Mod 2, y = Mod 3 Into x1 $$= Sum(y + 1), x2 = Sum(y + 2)|]
  end sub
end class</text>)
        End Sub

        <Fact>
        Public Sub TestGroupByNested()
            TestSpan(<text>
Imports System.Linq

Class C
    Public Class Customer
        Public CompanyName As String
        Public Orders As Order()
    End Class
    Public Class Order
        Public OrderDate As DateTime
    End Class
    Public Sub NestedGroupBy()
        Dim customers As List(Of Customer) = Nothing
        Dim customerOrderGroups = From c In customers
                                  Select New With {c.CompanyName,
                                                    .YearGroups =
                                                        From o In c.Orders
                                                        Group o By o.OrderDate.Year Into Group
                                                        Select New With {.Year = Year,
                                                                         .MonthGroups =
                                                                            From o In Group
                                                                            Group o By [|o.Order$$Date.Month|] Into MonthGroup = Group
                                                                            Select New With {.Month = Month,
                                                                                             .Orders = MonthGroup}}}
    End Sub
End Class
</text>)
        End Sub

#End Region
    End Class
End Namespace
