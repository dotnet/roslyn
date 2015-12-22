' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
