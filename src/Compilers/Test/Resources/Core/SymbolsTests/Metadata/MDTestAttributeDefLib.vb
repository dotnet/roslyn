' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'
' vbc /t:library /vbruntime- MDTestAttributeDefLib.vb
' 

' Defines some attributes classes that can be used for testing attribute metadata loading.

Public Enum TestAttributeEnum
    Yes
    No
    Maybe
End Enum

<AttributeUsage(AttributeTargets.All)>
Public Class AObjectAttribute
    Inherits Attribute

    Sub New(b As Object)
    End Sub

    Sub New(b As Object())
    End Sub

    Public Property O As Object

    Public Property OA As Object()

End Class

<AttributeUsage(AttributeTargets.All)>
Public Class AEnumAttribute
    Inherits Attribute

    Sub New(e As TestAttributeEnum)
    End Sub

    Sub New(e As TestAttributeEnum())
    End Sub

    Public Property E As TestAttributeEnum

    Public Property EA As TestAttributeEnum()

End Class

<AttributeUsage(AttributeTargets.All)>
Public Class ABooleanAttribute
    Inherits Attribute

    Sub New(b As Boolean)
    End Sub

    Sub New(b As Boolean())
    End Sub

    Public Property B As Boolean

End Class

<AttributeUsage(AttributeTargets.All)>
Public Class AByteAttribute
    Inherits Attribute

    Sub New(i As Byte)
    End Sub

    Sub New(i As Byte())
    End Sub

    Public Property B As Byte

End Class


<AttributeUsage(AttributeTargets.All)>
Public Class AInt16Attribute
    Inherits Attribute

    Sub New(i As Int16)
    End Sub

    Sub New(i As Int16())
    End Sub

    Public Property I As Int16

    Public Property IA As Int16()

End Class


<AttributeUsage(AttributeTargets.All)>
Public Class AInt32Attribute
    Inherits Attribute

    Sub New(i As Int32)
    End Sub

    Sub New(i As Int32())
    End Sub

    Public Property I As Int32

    Public Property IA As Int32()

End Class


<AttributeUsage(AttributeTargets.All)>
Public Class AInt64Attribute
    Inherits Attribute

    Sub New(i As Int64)
    End Sub

    Sub New(i As Int64())
    End Sub

    Public Property I As Int64

    Public Property IA As Int64()

End Class


<AttributeUsage(AttributeTargets.All)>
Public Class AUInt16Attribute
    Inherits Attribute

    Sub New(i As UInt16)
    End Sub

    Sub New(i As UInt16())
    End Sub

    Public Property U As UInt16

    Public Property UA As UInt16()

End Class


<AttributeUsage(AttributeTargets.All)>
Public Class AUint32Attribute
    Inherits Attribute

    Sub New(i As UInt32)
    End Sub

    Sub New(i As UInt32())
    End Sub

    Public Property U As Int32

    Public Property UA As UInt32()
End Class

<AttributeUsage(AttributeTargets.All)>
Public Class AUint64Attribute
    Inherits Attribute

    Sub New(i As UInt64)
    End Sub

    Sub New(i As UInt64())
    End Sub

    Public Property U As Int64

    Public Property UA As UInt64()

End Class

<AttributeUsage(AttributeTargets.All)>
Public Class ASingleAttribute
    Inherits Attribute

    Sub New(i As Single)
    End Sub

    Sub New(i As Single())
    End Sub

    Public Property S As Single

    Public Property SA As Single()

End Class

<AttributeUsage(AttributeTargets.All)>
Public Class ADoubleAttribute
    Inherits Attribute

    Sub New(i As Double)
    End Sub

    Sub New(i As Double())
    End Sub

    Public Property D As Double

    Public Property DA As Double()

End Class

<AttributeUsage(AttributeTargets.All)>
Public Class ACharAttribute
    Inherits Attribute

    Sub New(i As Char)
    End Sub

    Sub New(i As Char())
    End Sub

    Public Property C As Char

    Public Property CA As Char()

End Class

<AttributeUsage(AttributeTargets.All)>
Public Class AStringAttribute
    Inherits Attribute

    Sub New ()
    End Sub

    Sub New(s As String)
    End Sub

    Sub New(s As String())
    End Sub

    Public Property S As String

    Public Property SA As String()

    Public sField As String

End Class

<AttributeUsage(AttributeTargets.All, AllowMultiple:= true)>
Public Class ATypeAttribute
    Inherits Attribute

    Sub New(t As Type)
    End Sub

    Sub New(t As Type())
    End Sub

    Public T As Type

    Public TA As Type()
End Class

Public Class TopLevelClass
    <AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
    Public Class ANestedAttribute
        Inherits Attribute

        Sub New(b As Boolean)
        End Sub
    End Class
End Class









