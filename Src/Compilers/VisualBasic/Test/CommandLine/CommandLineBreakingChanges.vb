' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit
Imports Microsoft.CodeAnalysis.Test.Utilities.SharedResourceHelpers

Partial Public Class CommandLineTests
    <Fact, WorkItem(530256, "DevDiv")>
    Public Sub WarnAsErrorPrecedence1()
        Dim src As String = Temp.CreateFile().WriteAllText(<text>
Module Module1
    Sub Main()
        Dim x
        Console.WriteLine(x.ToString)
    End Sub
End Module
</text>.Value).Path
        Dim tempBinary = Temp.CreateFile()
        Dim tempLog = Temp.CreateFile()
        Dim output = RunAndGetOutput("cmd", "/C """ & BasicCompilerCommand & " /nologo /warnaserror+ /warnaserror- /out:" & tempBinary.Path & " " & src & " > " & tempLog.Path & """", expectedRetCode:=0)
        Assert.Equal("", output.Trim())

        'See bug 15593. This is not strictly a 'breaking' change.
        'In Dev11, /warnaserror+ trumps /warnaserror- in the above case and warnings are reported as errors.
        'In Roslyn, /warnaserror- (i.e. last one) wins.
        Assert.Equal(<text>
SRC.VB(5) : warning BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.

        Console.WriteLine(x.ToString)
                          ~
</text>.Value.Trim().Replace(vbLf, vbCrLf), tempLog.ReadAllText().Trim().Replace(src, "SRC.VB"))

        CleanupAllGeneratedFiles(src)
        CleanupAllGeneratedFiles(tempBinary.Path)
        CleanupAllGeneratedFiles(tempLog.Path)
    End Sub

    <Fact, WorkItem(530668, "DevDiv")>
    Public Sub WarnAsErrorPrecedence2()
        Dim src As String = Temp.CreateFile().WriteAllText(<text>
Module M1
    Sub Main
    End Sub
    Sub M(a as Object)
        if (a.Something &lt;&gt; 2)
        end if
    End Sub
End Module
</text>.Value).Path
        Dim tempOut = Temp.CreateFile()
        Dim output = RunAndGetOutput("cmd", "/C """ & BasicCompilerCommand & " /nologo /optionstrict:custom /nowarn:41008 /warnaserror+ " & src & " > " & tempOut.Path & """", expectedRetCode:=1)
        Assert.Equal("", output.Trim())

        'See bug 16673.
        'In Dev11, /warnaserror+ does not come into effect strangely and the code only reports warnings.
        'In Roslyn, /warnaserror+ does come into effect and the code reports the warnings as errors.

        Assert.Equal(<text>
SRC.VB(6) : error BC42017: Late bound resolution; runtime errors could occur.

        if (a.Something &lt;&gt; 2)
            ~~~~~~~~~~~      
SRC.VB(6) : error BC31072: Warning treated as error : Late bound resolution; runtime errors could occur.

        if (a.Something &lt;&gt; 2)
            ~~~~~~~~~~~      
SRC.VB(6) : error BC42032: Operands of type Object used for operator '&lt;&gt;'; use the 'IsNot' operator to test object identity.

        if (a.Something &lt;&gt; 2)
            ~~~~~~~~~~~      
SRC.VB(6) : error BC42016: Implicit conversion from 'Object' to 'Boolean'.

        if (a.Something &lt;&gt; 2)
           ~~~~~~~~~~~~~~~~~~
</text>.Value.Trim().Replace(vbLf, vbCrLf), tempOut.ReadAllText().Trim().Replace(src, "SRC.VB"))
    End Sub

    <Fact, WorkItem(530668, "DevDiv")>
    Public Sub WarnAsErrorPrecedence3()
        Dim src As String = Temp.CreateFile().WriteAllText(<text>
Module M1
    Sub Main
    End Sub
    Sub M(a as Object)
        if (a.Something &lt;&gt; 2)
        end if
    End Sub
End Module
</text>.Value).Path
        Dim tempOut = Temp.CreateFile()
        Dim output = RunAndGetOutput("cmd", "/C """ & BasicCompilerCommand & " /nologo /optionstrict:custom /warnaserror-:42025 /warnaserror+ " & src & " > " & tempOut.Path & """", expectedRetCode:=1)
        Assert.Equal("", output.Trim())

        'See bug 16673.
        'In Dev11, /warnaserror+ does not come into effect strangely and the code only reports warnings.
        'In Roslyn, /warnaserror+ does come into effect and the code reports the warnings as errors.

        Assert.Equal(<text>
SRC.VB(6) : error BC42017: Late bound resolution; runtime errors could occur.

        if (a.Something &lt;&gt; 2)
            ~~~~~~~~~~~      
SRC.VB(6) : error BC31072: Warning treated as error : Late bound resolution; runtime errors could occur.

        if (a.Something &lt;&gt; 2)
            ~~~~~~~~~~~      
SRC.VB(6) : error BC42032: Operands of type Object used for operator '&lt;&gt;'; use the 'IsNot' operator to test object identity.

        if (a.Something &lt;&gt; 2)
            ~~~~~~~~~~~      
SRC.VB(6) : error BC42016: Implicit conversion from 'Object' to 'Boolean'.

        if (a.Something &lt;&gt; 2)
           ~~~~~~~~~~~~~~~~~~
</text>.Value.Trim().Replace(vbLf, vbCrLf), tempOut.ReadAllText().Trim().Replace(src, "SRC.VB"))

        CleanupAllGeneratedFiles(src)
        CleanupAllGeneratedFiles(tempOut.Path)
    End Sub
End Class
