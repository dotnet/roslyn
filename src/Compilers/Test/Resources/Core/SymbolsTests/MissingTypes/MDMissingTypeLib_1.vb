' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

<Assembly: System.Reflection.AssemblyVersion("2.0.0.0")> 
<Assembly: System.Reflection.AssemblyFileVersion("2.0.0.0")> 

Namespace MissingNS1
    Public Class MissingC1
    End Class
End Namespace

Namespace MissingNS2
    Namespace MissingNS3
        Public Class MissingC2
        End Class
    End Namespace
End Namespace

Namespace NS4
    Namespace MissingNS5
        Public Class MissingC3
        End Class
    End Namespace
End Namespace

Public Class MissingC4(Of T, S)
    Public Class MissingC5(Of U, V, W)
    End Class
End Class

Public Class C6
    Public Class MissingC7(Of T, S)
        Public Class MissingC8
            Public Class MissingC9
            End Class
        End Class
    End Class
End Class
