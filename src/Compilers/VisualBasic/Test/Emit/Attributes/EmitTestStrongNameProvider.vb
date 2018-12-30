' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

    <Fact>
    Public Sub BadInputStream()
        SigningTestHelpers.InstallKey()
        Dim thrownException = New IOException("This is a test IOException")
        Dim testProvider = New TestDesktopStrongNameProvider() With {
            .SignFileFunc = Sub(a, b) Throw thrownException
        }

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
