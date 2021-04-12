' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Xunit

Public Class MiscTests

    ''' <summary>
    ''' Sanity check to help ensure our code base was compiled without overflow checking.
    ''' </summary>
    <Fact>
    Public Sub OverflowCheck()
        Dim max = Integer.MaxValue
        Dim x = max + max
        Assert.Equal(-2, x)
        Dim y = 0 - max
        Assert.Equal(-2147483647, y)
    End Sub

End Class
