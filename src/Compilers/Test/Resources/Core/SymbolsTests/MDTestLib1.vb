' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.


'
' vbc /t:library /vbruntime- MDTestLib1.vb
' 

Public Class C1(Of C1_T) 

    Public Class C2(Of C2_T)

    End Class

    Public Class C3
        Public Class C4(Of C4_T)

        End Class

    End Class
End Class


Public Class TC2(Of TC2_T1, TC2_T2)
    Inherits C1(Of TC2_T1).C2(Of TC2_T2)
End Class

Public Class TC3(Of TC3_T1)
    Inherits C1(Of TC3_T1).C3
End Class

Public Class TC4(Of TC4_T1, TC4_T2)
    Inherits C1(Of TC4_T1).C3.C4(Of TC4_T2)
End Class

Public Interface C100(Of Out T)
End Interface

Public Interface C101(Of In T)
End Interface

Public Class C102(Of T As New)
End Class

Public Class C103(Of T As Class)
End Class

Public Class C104(Of T As Structure)
End Class

Public Class C105(Of T As {New, Class})
End Class

Public Interface C106(Of Out T As {New, Class})
End Interface

Public Class C107
    Public Class C108(Of C108_T)
    End Class

End Class

Public Class C201(Of T As I101)

End Class

Public Class C202(Of T As {I101, I102})

End Class

Public Class C203
    Implements I101
End Class

Public Class C204
    Implements I101, I102
End Class


Public Interface I101

End Interface

Public Interface I102

End Interface
