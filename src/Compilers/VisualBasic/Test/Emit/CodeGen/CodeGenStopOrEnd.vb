' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenStopOrEnd
        Inherits BasicTestBase

        <Fact>
        Public Sub StopStatement_SimpleTestWithStop()
            Dim Source = <compilation>
                             <file name="a.vb">
                Imports System
                Imports  Microsoft.VisualBasic

                Public Module Module1
                    Public Sub Main()
                        Console.Writeline("Start")
                        Stop
                        Console.Writeline("End")
                    End Sub
                End Module
                    </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(Source, TestOptions.ReleaseExe)
            Dim compilationVerifier = CompileAndVerify(compilation).VerifyIL("Module1.Main",
            <![CDATA[{
  // Code size       26 (0x1a)
  .maxstack  1
  IL_0000:  ldstr      "Start"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  call       "Sub System.Diagnostics.Debugger.Break()"
  IL_000f:  ldstr      "End"
  IL_0014:  call       "Sub System.Console.WriteLine(String)"
  IL_0019:  ret
}]]>)

        End Sub

        <Fact>
        Public Sub StopStatement_SimpleTestWithEndOtherThanMain()
            Dim Source = <compilation>
                             <file name="a.vb">
                Imports System
                Imports  Microsoft.VisualBasic

                Public Module Module1
                    Public Sub Main()
                        Console.Writeline("Start")
                        Goo()
                        Console.Writeline("End")
                    End Sub

                    Sub Goo
                        Console.Writeline("Goo")
                        Stop
                    End Sub
                End Module
                    </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(Source, TestOptions.ReleaseExe)
            Dim compilationVerifier = CompileAndVerify(compilation).VerifyIL("Module1.Goo",
            <![CDATA[{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      "Goo"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  call       "Sub System.Diagnostics.Debugger.Break()"
  IL_000f:  ret
}]]>)
        End Sub

        <Fact>
        Public Sub StopStatement_MultipleStatementsOnASingleLine()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
                Imports System
                Imports  Microsoft.VisualBasic

                Public Module Module1
                    Public Sub Main()
                        Console.Writeline("Start")
                        Goo() : Stop
                        Console.Writeline("End")
                    End Sub

                    Sub Goo
                        Console.Writeline("Goo")
                    End Sub
                End Module
                    </file>
</compilation>).VerifyIL("Module1.Main",
            <![CDATA[{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldstr      "Start"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  call       "Sub Module1.Goo()"
  IL_000f:  call       "Sub System.Diagnostics.Debugger.Break()"
  IL_0014:  ldstr      "End"
  IL_0019:  call       "Sub System.Console.WriteLine(String)"
  IL_001e:  ret
}]]>)
        End Sub

        <Fact()>
        Public Sub StopStatement_CodeGenVerify()
            ' Ensure that IL contains a call to System.Diagnostics.Debugger.Break
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
        Imports System
        Imports  Microsoft.VisualBasic

        Public Module Module1
            Public Sub Main()
                Console.Writeline("Start")
                Goo()
                Console.Writeline("End")
            End Sub

            Sub Goo
                Console.Writeline("Goo")
                Stop
            End Sub
        End Module
            </file>
    </compilation>).VerifyIL("Module1.Goo",
            <![CDATA[{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      "Goo"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  call       "Sub System.Diagnostics.Debugger.Break()"
  IL_000f:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub EndStatement_SimpleTestWithEnd()
            ' Ensure that IL contains a call to Microsoft.VisualBasic.CompilerServices.ProjectData.EndAp
            Dim Source = <compilation>
                             <file name="a.vb">
        Imports System
        Imports  Microsoft.VisualBasic

        Public Module Module1
            Public Sub Main()
                Console.Writeline("Start")
                End
                Console.Writeline("End")
            End Sub
        End Module
            </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(Source, TestOptions.ReleaseExe)
            Dim compilationVerifier = CompileAndVerify(compilation).VerifyIL("Module1.Main",
            <![CDATA[{
  // Code size       26 (0x1a)
  .maxstack  1
  IL_0000:  ldstr      "Start"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.EndApp()"
  IL_000f:  ldstr      "End"
  IL_0014:  call       "Sub System.Console.WriteLine(String)"
  IL_0019:  ret
}]]>)
        End Sub

        <Fact, WorkItem(910884, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/910884")>
        Public Sub StopStatement_In_DebugMode()
            Dim Source = <compilation>
                             <file name="a.vb">
                Imports System
                Imports  Microsoft.VisualBasic

                Public Module Module1
                    Public Sub Main()
                        Stop
                        Console.Writeline("Hello")
                    End Sub
                End Module
                    </file>
                         </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(Source, TestOptions.ReleaseExe)
            Dim compilationVerifier = CompileAndVerify(compilation).VerifyIL("Module1.Main",
            <![CDATA[{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  call       "Sub System.Diagnostics.Debugger.Break()"
  IL_0005:  ldstr      "Hello"
  IL_000a:  call       "Sub System.Console.WriteLine(String)"
  IL_000f:  ret
}]]>)

            ' We are looking for a nop after a call to System.Diagnostics.Debugger.Break():
            compilation = CreateCompilationWithMscorlib40AndVBRuntime(Source, TestOptions.DebugExe)
            compilationVerifier = CompileAndVerify(compilation).VerifyIL("Module1.Main",
            <![CDATA[{
  // Code size       19 (0x13)
  .maxstack  1
  IL_0000:  nop
  IL_0001:  call       "Sub System.Diagnostics.Debugger.Break()"
  IL_0006:  nop
  IL_0007:  ldstr      "Hello"
  IL_000c:  call       "Sub System.Console.WriteLine(String)"
  IL_0011:  nop
  IL_0012:  ret
}]]>)

        End Sub
    End Class

End Namespace
