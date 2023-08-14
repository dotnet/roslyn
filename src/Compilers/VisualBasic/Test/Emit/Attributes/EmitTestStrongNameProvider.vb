' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection.Metadata
Imports System.Security.Cryptography
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.SigningTestHelpers

Partial Public Class InternalsVisibleToAndStrongNameTests
    Inherits BasicTestBase

    <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
    Public Sub BadInputStream()
        SigningTestHelpers.InstallKey()
        Dim thrownException = New IOException("This is a test IOException")
        Dim testFileSystem = New TestStrongNameFileSystem(Temp.CreateDirectory().Path) With {
            .CreateFileStreamFunc = Function(filePath As String, fileMode As FileMode, fileAccess As FileAccess, fileShare As FileShare) As FileStream
                                        Throw thrownException
                                    End Function
        }
        Dim testProvider = New TestDesktopStrongNameProvider(fileSystem:=testFileSystem)
        Dim options = TestOptions.DebugDll.WithStrongNameProvider(testProvider).WithCryptoKeyContainer("RoslynTestContainer")

        Dim comp = CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
Public Class C
    Public Sub M()
    End Sub
End Class
]]>
                </file>
            </compilation>, options:=options)

        comp.Emit(New MemoryStream()).Diagnostics.Verify(
            Diagnostic(ERRID.ERR_PeWritingFailure).WithArguments(thrownException.ToString()).WithLocation(1, 1))

    End Sub

End Class
