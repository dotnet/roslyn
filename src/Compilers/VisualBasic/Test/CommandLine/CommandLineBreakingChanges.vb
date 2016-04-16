' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports Roslyn.Test.Utilities.SharedResourceHelpers
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine.UnitTests

    Partial Public Class CommandLineTests

        <Fact, WorkItem(530256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530256")>
        Public Sub WarnAsErrorPrecedence1()
            Dim src As String = Temp.CreateFile().WriteAllText("
Imports System

Module Module1
    Sub Main()
        Dim x
        Console.WriteLine(x.ToString)
    End Sub
End Module
").Path
            Dim tempBinary = Temp.CreateFile()
            Dim tempLog = Temp.CreateFile()
            Dim output = ProcessUtilities.RunAndGetOutput("cmd", "/C """ & s_basicCompilerExecutable & """ /nologo /preferreduilang:en /warnaserror+ /warnaserror- /out:" & tempBinary.Path & " " & src & " > " & tempLog.Path, expectedRetCode:=0)
            Assert.Equal("", output.Trim())

            'See bug 15593. This is not strictly a 'breaking' change.
            'In Dev11, /warnaserror+ trumps /warnaserror- in the above case and warnings are reported as errors.
            'In Roslyn, /warnaserror- (i.e. last one) wins.
            Assert.Equal(<text>
SRC.VB(7) : warning BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.

        Console.WriteLine(x.ToString)
                          ~
</text>.Value.Trim().Replace(vbLf, vbCrLf), tempLog.ReadAllText().Trim().Replace(src, "SRC.VB"))

            CleanupAllGeneratedFiles(src)
            CleanupAllGeneratedFiles(tempBinary.Path)
            CleanupAllGeneratedFiles(tempLog.Path)
        End Sub

        <Fact, WorkItem(530668, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530668")>
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
            Dim output = ProcessUtilities.RunAndGetOutput("cmd", "/C """ & s_basicCompilerExecutable & """ /nologo /preferreduilang:en /optionstrict:custom /nowarn:41008 /warnaserror+ " & src & " > " & tempOut.Path, expectedRetCode:=1)
            Assert.Equal("", output.Trim())

            'See bug 16673.
            'In Dev11, /warnaserror+ does not come into effect strangely and the code only reports warnings.
            'In Roslyn, /warnaserror+ does come into effect and the code reports the warnings as errors.

            Assert.Equal(<text>
SRC.VB(6) : error BC42017: Late bound resolution; runtime errors could occur.

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

        <Fact, WorkItem(530668, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530668")>
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
            Dim output = ProcessUtilities.RunAndGetOutput("cmd", "/C """ & s_basicCompilerExecutable & """ /nologo /preferreduilang:en /optionstrict:custom /warnaserror-:42025 /warnaserror+ " & src & " > " & tempOut.Path, expectedRetCode:=1)
            Assert.Equal("", output.Trim())

            'See bug 16673.
            'In Dev11, /warnaserror+ does not come into effect strangely and the code only reports warnings.
            'In Roslyn, /warnaserror+ does come into effect and the code reports the warnings as errors.

            Assert.Equal(<text>
SRC.VB(6) : error BC42017: Late bound resolution; runtime errors could occur.

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
End Namespace
