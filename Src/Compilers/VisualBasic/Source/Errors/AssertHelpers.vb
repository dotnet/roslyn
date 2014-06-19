
Public Module AssertHelpers

    Sub ThrowIfNull(ByVal value As Integer)
        If (value = 0) Then
            Throw New NullReferenceException()
        End If
    End Sub

    Sub ThrowIfNull(ByVal value As UInteger)
        If (value = 0) Then
            Throw New NullReferenceException()
        End If
    End Sub

    Sub ThrowIfNull(ByVal value As Boolean)
        If Not value Then
            Throw New NullReferenceException()
        End If
    End Sub

    Sub ThrowIfNull(Of T As Class)(ByVal value As T)
        If (value Is Nothing) Then
            Throw New NullReferenceException()
        End If
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub VSASSERT(ByVal val As Boolean, Optional ByVal message As String = "")
        Debug.Assert(Val, message)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub VSASSERT(ByVal val As Integer, Optional ByVal message As String = "")
        Debug.Assert(val <> 0, message)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub VSASSERT(ByVal val As UInteger, Optional ByVal message As String = "")
        Debug.Assert(val <> 0, message)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub VSASSERT(Of T As Class)(ByVal val As T, Optional ByVal message As String = "")
        Debug.Assert(val IsNot Nothing, message)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub AssertIfNull(Of T As Class)(ByVal value As T)
        Debug.Assert(value IsNot Nothing)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub AssertIfFalse(Of T As Class)(ByVal value As T)
        Debug.Assert(value IsNot Nothing)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub AssertIfFalse(ByVal value As Boolean)
        Debug.Assert(value)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub AssertIfFalse(ByVal value As Boolean, ByVal message As String)
        Debug.Assert(value, message)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub AssertIfTrue(Of T As Class)(ByVal value As T)
        Debug.Assert(value Is Nothing)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub AssertIfTrue(ByVal value As Boolean)
        Debug.Assert(Not value)
    End Sub

    <ConditionalAttribute("DEBUG")> _
    Sub AssertIfTrue(ByVal value As UInteger)
        Debug.Assert(0 = value)
    End Sub

    Sub VSFAIL(ByVal val As String)
        Throw New NotSupportedException(val)
    End Sub

    Sub ThrowIfTrue(ByVal value As Boolean)
        Debug.Assert(Not value)
        If value Then
            Throw New NotSupportedException()
        End If
    End Sub

    Sub ThrowIfTrue(Of T As Class)(ByVal value As T)
        Debug.Assert(value Is Nothing)
        If value IsNot Nothing Then
            Throw New NotSupportedException()
        End If
    End Sub

    Sub ThrowIfFalse(ByVal value As Boolean)
        Debug.Assert(value)
        If Not value Then
            Throw New NotSupportedException()
        End If
    End Sub

    Sub ThrowIfFalse(Of T As Class)(ByVal value As T)
        Debug.Assert(value IsNot Nothing)
        If value Is Nothing Then
            Throw New NotSupportedException()
        End If
    End Sub

    Sub ThrowIfFalse(ByVal value As Integer)
        Debug.Assert(value <> 0)
        If value = 0 Then
            Throw New NotSupportedException()
        End If
    End Sub

End Module