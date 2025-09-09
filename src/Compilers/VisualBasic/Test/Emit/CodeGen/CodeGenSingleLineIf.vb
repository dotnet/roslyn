' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class CodeGenSingleLineIf
        Inherits BasicTestBase

        <Fact()>
        Public Sub SingleLineIf()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module M
    Sub Main()
        M(True, False)
        M(False, True)
    End Sub
    Sub M(e1 As Boolean, e2 As Boolean)
        Console.WriteLine("M({0}, {1})", e1, e2)
        If e1 Then M(1) : M(2)
        If e1 Then M(3) Else M(4)
        If e1 Then M(5) : Else M(6)
        If e1 Then Else M(7) : M(8)
        If e1 Then Else : M(9)
        If e1 Then If e2 Then Else M(10) Else M(11)
        If e1 Then If e2 Then Else Else M(12)
        If e1 Then Else If e2 Then Else M(13)
    End Sub
    Sub M(i As Integer)
        Console.WriteLine("{0}", i)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
M(True, False)
1
2
3
5
9
10
M(False, True)
4
6
7
8
9
11
12
]]>)
        End Sub

    End Class

End Namespace
